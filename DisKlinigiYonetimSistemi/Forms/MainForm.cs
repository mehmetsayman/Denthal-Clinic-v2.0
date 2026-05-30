using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Forms;

public sealed class MainForm : Form
{
    private readonly ClinicDataStore _store;
    private readonly UserAccount _currentUser;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = ModernUi.Background };
    private readonly System.Windows.Forms.Timer _syncTimer = new() { Interval = 15000 };
    private Action? _currentPage;
    private bool _syncInProgress;
    private int _lastAppointmentTabIndex;

    public MainForm(ClinicDataStore store, UserAccount currentUser)
    {
        _store = store;
        _currentUser = currentUser;
        DoubleBuffered = true;
        Text = $"ÇCETY Diş Kliniği - {_currentUser.FullName}";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1240, 780);
        BackColor = ModernUi.Background;
        Opacity = 0; // For fade-in animation
        Shown += (_, _) =>
        {
            var animator = new Animator(250);
            animator.Start(t => Opacity = t);
        };
        
        Font = ModernUi.BodyFont;
        BuildShell();
        Navigate(ShowDashboard);
        StartAutoSync();
        this.EnableDoubleBuffering();
    }

    private bool IsPatient => _currentUser.Role == UserRole.Hasta;
    private bool IsDoctor => _currentUser.Role == UserRole.Doktor;
    private bool IsSecretary => _currentUser.Role == UserRole.Sekreter;
    private bool IsAdmin => _currentUser.Role == UserRole.Admin;
    private bool CanClinical => IsAdmin || IsDoctor;
    private bool CanOffice => IsAdmin || IsSecretary;

    private void Navigate(Action action, bool remember = true)
    {
        if (remember)
        {
            _currentPage = action;
        }

        _content.SuspendDrawing();
        action();
        if (_content.Controls.Count > 0)
        {
            _content.Controls[0].EnableDoubleBuffering();
        }

        _content.ResumeDrawing();
    }

    private void StartAutoSync()
    {
        _syncTimer.Tick += async (_, _) => await PullCloudChangesAsync();
        _syncTimer.Start();
        FormClosed += (_, _) => _syncTimer.Stop();
    }

    private async Task PullCloudChangesAsync()
    {
        if (_syncInProgress || !_store.SupabaseEnabled || HasOpenModalDialog())
        {
            return;
        }

        _syncInProgress = true;
        try
        {
            var previousUpdate = _store.Snapshot.UpdatedAt;
            var snapshot = await _store.PullFromSupabaseAsync();
            var refresh = _currentPage;
            if (snapshot is not null && snapshot.UpdatedAt != previousUpdate && refresh is not null)
            {
                Navigate(refresh, remember: false);
            }
        }
        catch
        {
            // Offline kalırsa uygulama yerel cache ile çalışmaya devam eder.
        }
        finally
        {
            _syncInProgress = false;
        }
    }

    private bool HasOpenModalDialog()
    {
        foreach (Form form in Application.OpenForms)
        {
            if (form != this && form.Modal)
            {
                return true;
            }
        }

        return false;
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 34, 54),
            Padding = new Padding(16)
        };
        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(_content, 1, 0);

        var brand = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 220,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        brand.Controls.Add(ModernUi.Label("ÇCETY", new Font("Segoe UI Semibold", 24F), Color.White));
        brand.Controls.Add(ModernUi.Label("Diş Kliniği", new Font("Segoe UI Semibold", 14F), Color.FromArgb(187, 230, 224)));
        brand.Controls.Add(ModernUi.Label(RoleTitle(), ModernUi.SmallFont, Color.FromArgb(192, 206, 226)));
        brand.Controls.Add(ModernUi.Label(_currentUser.FullName, new Font("Segoe UI Semibold", 9.5F), Color.White));
        sidebar.Controls.Add(brand);

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 230, 0, 0),
            Margin = new Padding(0)
        };
        sidebar.Controls.Add(nav);
        AddNav(nav, "Ana Panel", ShowDashboard);
        AddNav(nav, IsPatient ? "Klinik Dosyam" : IsDoctor ? "Hasta Dosyaları" : "Hasta Merkezi", ShowPatientFiles);
        AddNav(nav, IsPatient ? "Randevu Al" : "Randevu Akışı", ShowAppointments);
        if (IsPatient) AddNav(nav, "Bildirimler", ShowNotifications);
        AddNav(nav, IsPatient ? "Reçetelerim" : "Akıllı Reçete", ShowPrescriptions);
        AddNav(nav, IsPatient ? "Röntgenlerim" : "Röntgen Arşivi", ShowRadiographs);
        AddNav(nav, IsPatient ? "Tedavilerim" : "Tedavi Süreci", ShowTreatments);
        if (!IsPatient) AddNav(nav, "Ekip Profilleri", ShowStaff);
        if (IsAdmin) AddNav(nav, "Sistem Logları", ShowLogs);
        if (IsAdmin) AddNav(nav, "Supabase Durumu", ShowSupabaseSettings, rememberPage: false);
        AddNav(nav, "Çıkış Yap", Close);
    }

    private string RoleTitle() => _currentUser.Role switch
    {
        UserRole.Admin => "Admin ve denetim paneli",
        UserRole.Doktor => $"{_currentUser.Specialty} doktor paneli",
        UserRole.Sekreter => $"{_store.DoctorName(_currentUser.AssignedDoctorUserId ?? "")} sekreteri",
        _ => "Hasta portalı"
    };

    private void AddNav(FlowLayoutPanel nav, string text, Action action, bool rememberPage = true)
    {
        var button = new Button
        {
            Text = text,
            Width = 230,
            Height = 46,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            Margin = new Padding(0, 5, 0, 5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(25, 34, 54),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 10F)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 56, 86);
        button.Click += (_, _) => 
        {
            if (text != "Çıkış Yap")
            {
                if (rememberPage)
                {
                    Navigate(action);
                }
                else
                {
                    action();
                }
            }
            else
            {
                action();
            }
        };
        nav.Controls.Add(button);
    }

    private void ShowSupabaseSettings()
    {
        using var dialog = new SupabaseSettingsForm(_store);
        dialog.ShowDialog(this);
    }

    private TableLayoutPanel Page(string title, string subtitle)
    {
        _content.Controls.Clear();
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(16), // Compact
            BackColor = ModernUi.Background
        };
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _content.Controls.Add(page);

        var header = new TableLayoutPanel 
        { 
            Dock = DockStyle.Fill, 
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 16)
        };
        header.Controls.Add(ModernUi.Label(title, ModernUi.TitleFont));
        header.Controls.Add(ModernUi.Label(subtitle, ModernUi.BodyFont, ModernUi.Muted));
        page.Controls.Add(header, 0, 0);
        return page;
    }

    private void ShowDashboard()
    {
        var page = Page("Ana Panel", DashboardSubtitle());
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(body, 0, 1);

        var appointments = VisibleAppointments().ToList();
        var treatments = VisibleTreatments().ToList();
        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        for (var i = 0; i < 4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stats.Controls.Add(MetricCard(IsPatient ? "Randevum" : "Hasta", IsPatient ? appointments.Count.ToString() : VisiblePatients().Count().ToString(), "Görünen kayıt", ModernUi.Primary), 0, 0);
        stats.Controls.Add(MetricCard("Bekleyen", appointments.Count(a => a.Status == AppointmentStatus.TalepEdildi).ToString(), "Onay bekleyen talep", ModernUi.Warning), 1, 0);
        stats.Controls.Add(MetricCard("Reçete", VisiblePrescriptions().Count().ToString(), "Kayıtlı reçete", Color.FromArgb(92, 107, 192)), 2, 0);
        stats.Controls.Add(MetricCard(IsPatient ? "Tedavi" : "Log", IsPatient ? treatments.Count.ToString() : VisibleLogs().Count().ToString(), IsPatient ? "Tedavi kaydı" : "İşlem kaydı", ModernUi.Accent), 3, 0);
        body.Controls.Add(stats, 0, 0);

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        body.Controls.Add(split, 0, 1);

        split.Controls.Add(Section("Yaklasan Randevular", Flow(UpcomingAppointments(appointments).Take(6).Select(a => AppointmentCard(a, true)))), 0, 0);
        split.Controls.Add(IsPatient
            ? Section("Klinik Özeti", PatientDashboardSummary())
            : Section(IsAdmin ? "Son Loglar" : "Kısa Klinik Akışı", Flow(VisibleLogs().Take(8).Select(LogCard))), 1, 0);
    }

    private string DashboardSubtitle() => _currentUser.Role switch
    {
        UserRole.Hasta => "Randevularınız, reçeteleriniz ve klinik dosyanız tek ekranda.",
        UserRole.Doktor => "Kendi hastalarınızı, reçetelerinizi ve randevu akışınızı yönetin.",
        UserRole.Sekreter => "Bağlı doktorunuzun hasta ve randevu operasyonlarını takip edin.",
        _ => "Kliniğin tüm operasyon, ekip ve log akışını denetleyin."
    };

    private static IEnumerable<Appointment> UpcomingAppointments(IEnumerable<Appointment> appointments) =>
        appointments
            .Where(appointment => appointment.StartsAt >= DateTime.Today &&
                                  appointment.Status is AppointmentStatus.TalepEdildi or AppointmentStatus.Onaylandi or AppointmentStatus.Geldi)
            .OrderBy(appointment => appointment.StartsAt);

    private Control PatientDashboardSummary()
    {
        var patient = VisiblePatients().FirstOrDefault();
        if (patient is null)
        {
            return Flow([]);
        }

        var latestPrescription = VisiblePrescriptions().OrderByDescending(prescription => prescription.Date).FirstOrDefault();
        var latestTreatment = VisibleTreatments().OrderByDescending(treatment => treatment.Date).FirstOrDefault();
        var latestRadiograph = VisibleRadiographs().OrderByDescending(radiograph => radiograph.Date).FirstOrDefault();

        return Flow([
            InfoCard("Risk", string.IsNullOrWhiteSpace(patient.RiskLevel) ? "-" : patient.RiskLevel, ModernUi.Warning),
            InfoCard("Alerji", string.IsNullOrWhiteSpace(patient.AllergyNotes) ? "-" : patient.AllergyNotes, ModernUi.Danger),
            InfoCard("Son Reçete", latestPrescription is null ? "Kayıt yok" : $"{latestPrescription.Topic} - {latestPrescription.Date:dd.MM.yyyy}", Color.FromArgb(92, 107, 192)),
            InfoCard("Son Tedavi", latestTreatment is null ? "Kayıt yok" : latestTreatment.ProcedureName, ModernUi.Accent),
            InfoCard("Son Röntgen", latestRadiograph is null ? "Kayıt yok" : $"{latestRadiograph.ToothRegion} - {latestRadiograph.Date:dd.MM.yyyy}", ModernUi.Primary)
        ]);
    }

    private async void EditCurrentPatient(Patient patient)
    {
        using var dialog = PatientEditorForm.Create(_store, patient);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Patient is null)
        {
            return;
        }

        var index = _store.Snapshot.Patients.FindIndex(item => item.Id == patient.Id);
        if (index < 0)
        {
            return;
        }

        var updated = dialog.Patient;
        _store.Snapshot.Patients[index] = updated;
        var linkedUser = _store.Snapshot.Users.FirstOrDefault(user => user.LinkedPatientId == updated.Id);
        if (linkedUser is not null)
        {
            linkedUser.FullName = updated.FullName;
            linkedUser.Email = updated.Email;
            linkedUser.Phone = updated.Phone;
        }

        Text = $"ÇCETY Diş Kliniği - {_currentUser.FullName}";
        await _store.AddLogAsync(_currentUser, "Bilgi Güncelleme", $"{updated.FullName} hasta bilgilerini güncelledi.", updated.Id);
        ShowPatientFiles();
    }

    private void ShowPatientFiles()
    {
        var patients = VisiblePatients().OrderBy(p => p.FullName).ToList();
        var page = Page(IsPatient ? "Klinik Dosyam" : "Hasta Dosyaları", IsPatient ? "Tüm sağlık ve diş kliniği bilgileriniz." : "Hastanın üzerine gelerek veya tıklayarak dosya detayları arasında gezinin.");

        if (IsPatient)
        {
            var patient = patients.FirstOrDefault();
            if (patient is not null)
            {
                page.Controls.Add(PatientDetail(patient), 0, 1);
            }
            return;
        }

        var master = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        master.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        master.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.Controls.Add(master, 0, 1);

        var list = FlowPanel();
        var detailHost = new Panel { Dock = DockStyle.Fill };
        
        var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, !IsPatient ? 58 : 0));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        if (!IsPatient)
        {
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var add = PrimaryAction("Yeni Hasta Ekle", 160);
            add.Click += async (_, _) => 
            {
                var p = EntityEditorForms.Patient(null);
                if (p is not null)
                {
                    _store.Snapshot.Patients.Add(p);
                    await _store.AddLogAsync(_currentUser, "Hasta Kaydı", $"{p.FullName} sisteme eklendi.", p.Id);
                    ShowPatientFiles();
                }
            };
            toolbar.Controls.Add(add);
            leftPanel.Controls.Add(toolbar, 0, 0);
        }
        leftPanel.Controls.Add(Section("Hasta Listesi", list), 0, 1);

        master.Controls.Add(leftPanel, 0, 0);
        master.Controls.Add(detailHost, 1, 0);

        Patient? currentRenderedPatient = null;

        void Render(Patient patient)
        {
            if (currentRenderedPatient == patient) return;
            currentRenderedPatient = patient;

            detailHost.Controls.Clear();
            detailHost.Controls.Add(PatientDetail(patient));
        }

        foreach (var patient in patients)
        {
            var card = PatientListCard(patient, () => Render(patient));
            list.Controls.Add(card);
        }

        if (patients.Count > 0) Render(patients[0]);
    }

    private Control PatientDetail(Patient patient)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var profile = ModernUi.Card();
        profile.Dock = DockStyle.Fill;
        var profileGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        profileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        profileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        profileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        profileGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        profileGrid.Controls.Add(Avatar(patient.FullName, 120), 0, 0);
        profileGrid.Controls.Add(Stack(ProfileLine("Hasta", patient.FullName, 18), ProfileLine("TC", patient.TcNo), ProfileLine("Telefon", patient.Phone)), 1, 0);
        profileGrid.Controls.Add(Stack(ProfileLine("Kan Grubu", patient.BloodType, 18), ProfileLine("Boy / Kilo", $"{patient.HeightCm} cm / {patient.WeightKg} kg"), ProfileLine("Risk", patient.RiskLevel)), 2, 0);
        var emergencyInfo = new List<Control>
        {
            ProfileLine("Acil Kişi", patient.EmergencyContactName, 18),
            ProfileLine("Acil Telefon", patient.EmergencyContactPhone),
            ProfileLine("Kayıt", patient.CreatedAt.ToString("dd.MM.yyyy"))
        };
        if (IsPatient)
        {
            var edit = PrimaryAction("Bilgilerimi Düzenle", 180);
            edit.AutoSize = true;
            edit.Margin = new Padding(0, 10, 0, 0);
            edit.Click += (_, _) => EditCurrentPatient(patient);
            emergencyInfo.Add(edit);

            var sendMail = PrimaryAction("Bilgilerimi E-postama Gönder", 230);
            sendMail.AutoSize = true;
            sendMail.Margin = new Padding(0, 10, 0, 0);
            sendMail.Click += async (_, _) => await SendPatientInfoToEmail(patient);
            emergencyInfo.Add(sendMail);
        }
        else
        {
            var edit = PrimaryAction("Bilgileri Düzenle", 180);
            edit.AutoSize = true;
            edit.Margin = new Padding(0, 10, 0, 0);
            edit.Click += async (_, _) => 
            {
                var p = EntityEditorForms.Patient(patient);
                if (p is not null)
                {
                    var idx = _store.Snapshot.Patients.FindIndex(x => x.Id == patient.Id);
                    if (idx >= 0) _store.Snapshot.Patients[idx] = p;
                    await _store.AddLogAsync(_currentUser, "Bilgi Güncelleme", $"{p.FullName} bilgileri güncellendi.", p.Id);
                    ShowPatientFiles();
                }
            };
            emergencyInfo.Add(edit);

            if (IsAdmin || IsDoctor)
            {
                var del = PrimaryAction("Hastayı Sil", 180);
                del.BackColor = ModernUi.Danger;
                del.Margin = new Padding(0, 10, 0, 0);
                del.Click += async (_, _) => 
                {
                    if (MessageBox.Show($"{patient.FullName} adlı hastayı silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        _store.Snapshot.Patients.Remove(patient);
                        await _store.AddLogAsync(_currentUser, "Hasta Silme", $"{patient.FullName} silindi.");
                        ShowPatientFiles();
                    }
                };
                emergencyInfo.Add(del);
            }
        }
        profileGrid.Controls.Add(Stack(emergencyInfo.ToArray()), 3, 0);
        profile.Controls.Add(profileGrid);
        root.Controls.Add(profile, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = ModernUi.BodyFont };
        tabs.TabPages.Add(Tab("Genel", Flow([
            InfoCard("Alerji", patient.AllergyNotes, ModernUi.Danger),
            InfoCard("Kronik Hastalık", patient.ChronicDiseases, ModernUi.Warning),
            InfoCard("Kullandığı İlaçlar", patient.CurrentMedications, Color.FromArgb(92, 107, 192)),
            InfoCard("Sigara", patient.SmokingStatus, ModernUi.Muted),
            InfoCard("Diş Geçmişi", patient.DentalHistory, ModernUi.Primary),
            InfoCard("Adres", patient.Address, ModernUi.Accent)
        ])));
        tabs.TabPages.Add(Tab("Randevular", Flow(VisibleAppointments().Where(a => a.PatientId == patient.Id).OrderBy(a => a.StartsAt).Select(a => AppointmentCard(a, true)))));
        if (IsPatient)
        {
            tabs.TabPages.Add(Tab("Bildirimler", Flow(VisibleNotifications().Select(NotificationCard))));
        }
        tabs.TabPages.Add(Tab("Reçeteler", Flow(VisiblePrescriptions().Where(p => p.PatientId == patient.Id).OrderByDescending(p => p.Date).Select(PrescriptionCard))));
        tabs.TabPages.Add(Tab("Röntgenler", Flow(VisibleRadiographs().Where(r => r.PatientId == patient.Id).OrderByDescending(r => r.Date).Select(RadiographCard))));
        tabs.TabPages.Add(Tab("Tedavi", Flow(VisibleTreatments().Where(t => t.PatientId == patient.Id).OrderByDescending(t => t.Date).Select(TreatmentCard))));
        if (!IsPatient)
        {
            tabs.TabPages.Add(Tab("Loglar", Flow(VisibleLogs().Where(l => l.PatientId == patient.Id).Take(20).Select(LogCard))));
        }
        root.Controls.Add(tabs, 0, 1);
        return root;
    }

    private void ShowAppointments()
    {
        var page = Page(IsPatient ? "Randevu Al" : "Randevu Akışı", IsPatient ? "Yeni talep oluşturun, onay durumunu takip edin." : "Bekleyen talepleri kartlardan yönetin.");
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(body, 0, 1);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var add = PrimaryAction(IsPatient ? "Yeni Randevu Talebi" : "Randevu Ekle", 190);
        add.Click += (_, _) => AddAppointment();
        toolbar.Controls.Add(add);
        body.Controls.Add(toolbar, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = ModernUi.BodyFont };
        var appointments = VisibleAppointments().ToList();
        tabs.TabPages.Add(Tab("Bekleyen", Flow(appointments.Where(a => a.Status == AppointmentStatus.TalepEdildi).OrderBy(a => a.StartsAt).Select(a => AppointmentCard(a, true)))));
        tabs.TabPages.Add(Tab("Onaylı", Flow(appointments.Where(a => a.Status == AppointmentStatus.Onaylandi).OrderBy(a => a.StartsAt).Select(a => AppointmentCard(a, true)))));
        tabs.TabPages.Add(Tab("Geçmiş", Flow(appointments.Where(a => a.Status is AppointmentStatus.Tamamlandi or AppointmentStatus.Geldi or AppointmentStatus.Iptal or AppointmentStatus.Reddedildi).OrderByDescending(a => a.StartsAt).Select(a => AppointmentCard(a, true)))));
        
        tabs.SelectedIndexChanged += (_, _) => _lastAppointmentTabIndex = tabs.SelectedIndex;
        if (_lastAppointmentTabIndex >= 0 && _lastAppointmentTabIndex < tabs.TabCount)
        {
            tabs.SelectedIndex = _lastAppointmentTabIndex;
        }
        
        body.Controls.Add(tabs, 0, 1);
    }

    private void ShowNotifications()
    {
        var page = Page("Bildirimler", "Randevu ve hesap hareketleriniz burada listelenir.");
        page.Controls.Add(Section("Gelen Bildirimler", Flow(VisibleNotifications().Select(NotificationCard))), 0, 1);
    }

    private void ShowPrescriptions()
    {
        var page = Page(IsPatient ? "Reçetelerim" : "Akıllı Reçete", IsPatient ? "Doktor tarafından yazılan reçeteleriniz." : "İlaç kataloğundan seçim yapın, kullanım talimatı otomatik gelsin.");
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, CanClinical ? 58 : 0));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(body, 0, 1);

        if (CanClinical)
        {
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var add = PrimaryAction("Yeni Reçete Yaz", 160);
            add.Click += (_, _) => AddPrescription();
            toolbar.Controls.Add(add);
            body.Controls.Add(toolbar, 0, 0);
        }

        body.Controls.Add(Section("Reçete Kartları", Flow(VisiblePrescriptions().OrderByDescending(p => p.Date).Select(PrescriptionCard))), 0, 1);
    }

    private void ShowRadiographs()
    {
        var page = Page(IsPatient ? "Röntgenlerim" : "Röntgen Arşivi", "Röntgenler görsel önizleme ve klinik notlarla listelenir.");
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, CanClinical ? 58 : 0));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(body, 0, 1);

        if (CanClinical)
        {
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var add = PrimaryAction("Röntgen Ekle", 140);
            add.Click += (_, _) => AddRadiograph();
            toolbar.Controls.Add(add);
            body.Controls.Add(toolbar, 0, 0);
        }
        body.Controls.Add(Section("Görsel Arşiv", Flow(VisibleRadiographs().OrderByDescending(r => r.Date).Select(RadiographCard))), 0, 1);
    }

    private void ShowTreatments()
    {
        var page = Page(IsPatient ? "Tedavilerim" : "Tedavi Süreci", "Klinik tedavi notları, diş numarası ve durum bilgisi.");
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, CanClinical ? 58 : 0));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(body, 0, 1);

        if (CanClinical)
        {
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var add = PrimaryAction("Tedavi Notu Ekle", 160);
            add.Click += (_, _) => AddTreatment();
            toolbar.Controls.Add(add);
            body.Controls.Add(toolbar, 0, 0);
        }
        body.Controls.Add(Section("Tedavi Kartları", Flow(VisibleTreatments().OrderByDescending(t => t.Date).Select(TreatmentCard))), 0, 1);
    }

    private void ShowStaff()
    {
        var page = Page("Ekip Profilleri", "Her doktorun sorumlu sekreteri ve klinik profili.");
        
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, IsAdmin ? 58 : 0));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        if (IsAdmin)
        {
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var add = PrimaryAction("Yeni Doktor Ekle", 160);
            add.Click += async (_, _) => 
            {
                var (d, secId) = EntityEditorForms.Doctor(_store, null);
                if (d is not null)
                {
                    _store.Snapshot.Users.Add(d);
                    if (!string.IsNullOrWhiteSpace(secId))
                    {
                        var sec = _store.Snapshot.Users.FirstOrDefault(u => u.Id == secId);
                        if (sec is not null)
                        {
                            sec.AssignedDoctorUserId = d.Id;
                        }
                    }
                    await _store.AddLogAsync(_currentUser, "Doktor Ekleme", $"{d.FullName} sisteme eklendi.", null, d.Id);
                    ShowStaff();
                }
            };
            toolbar.Controls.Add(add);
            body.Controls.Add(toolbar, 0, 0);
        }

        var flow = FlowPanel();
        var allDoctors = _store.Snapshot.Users.Where(u => u.Role == UserRole.Doktor).OrderByDescending(u => u.Active).ThenBy(u => u.FullName).ToList();
        foreach (var doctor in allDoctors)
        {
            flow.Controls.Add(DoctorCard(doctor));
        }
        body.Controls.Add(Section("Doktor - Sekreter Eşleşmeleri", flow), 0, 1);
        page.Controls.Add(body, 0, 1);
    }

    private void ShowLogs()
    {
        var page = Page("Sistem Logları", "Sistemde yapılan tüm kritik hareketler.");
        page.Controls.Add(Section("Denetim Akışı", Flow(VisibleLogs().Take(100).Select(LogCard))), 0, 1);
    }

    private IEnumerable<Patient> VisiblePatients()
    {
        if (IsPatient)
        {
            var patient = CurrentPatient();
            return patient is null ? Enumerable.Empty<Patient>() : new[] { patient };
        }

        if (IsDoctor)
        {
            var patientIds = ClinicalPatientIds(_currentUser.Id);
            return _store.Snapshot.Patients.Where(p => patientIds.Contains(p.Id));
        }

        if (IsSecretary && _currentUser.AssignedDoctorUserId is not null)
        {
            var patientIds = ClinicalPatientIds(_currentUser.AssignedDoctorUserId);
            return _store.Snapshot.Patients.Where(p => patientIds.Contains(p.Id));
        }

        return _store.Snapshot.Patients;
    }

    private Patient? CurrentPatient()
    {
        if (!IsPatient)
        {
            return null;
        }

        var patient = !string.IsNullOrWhiteSpace(_currentUser.LinkedPatientId)
            ? _store.Snapshot.Patients.FirstOrDefault(item => item.Id == _currentUser.LinkedPatientId)
            : null;

        patient ??= _store.Snapshot.Patients.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(_currentUser.UserName) &&
            item.TcNo.Equals(_currentUser.UserName, StringComparison.OrdinalIgnoreCase));

        patient ??= _store.Snapshot.Patients.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(_currentUser.Email) &&
            item.Email.Equals(_currentUser.Email, StringComparison.OrdinalIgnoreCase));

        patient ??= _store.Snapshot.Patients.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(_currentUser.FullName) &&
            item.FullName.Equals(_currentUser.FullName, StringComparison.OrdinalIgnoreCase));

        if (patient is not null)
        {
            _currentUser.LinkedPatientId = patient.Id;
            var storedUser = _store.Snapshot.Users.FirstOrDefault(user => user.Id == _currentUser.Id);
            if (storedUser is not null)
            {
                storedUser.LinkedPatientId = patient.Id;
            }
        }

        return patient;
    }

    private HashSet<string> ClinicalPatientIds(string doctorId) =>
        _store.Snapshot.Appointments.Where(a => a.DoctorUserId == doctorId).Select(a => a.PatientId)
            .Concat(_store.Snapshot.Prescriptions.Where(p => p.DoctorUserId == doctorId).Select(p => p.PatientId))
            .Concat(_store.Snapshot.Treatments.Where(t => t.DoctorUserId == doctorId).Select(t => t.PatientId))
            .Concat(_store.Snapshot.Radiographs.Where(r => r.DoctorUserId == doctorId).Select(r => r.PatientId))
            .ToHashSet();

    private IEnumerable<Appointment> VisibleAppointments()
    {
        if (IsPatient)
        {
            var patient = CurrentPatient();
            return patient is null ? Enumerable.Empty<Appointment>() : _store.Snapshot.Appointments.Where(a => a.PatientId == patient.Id);
        }
        if (IsDoctor) return _store.Snapshot.Appointments.Where(a => a.DoctorUserId == _currentUser.Id);
        if (IsSecretary && _currentUser.AssignedDoctorUserId is not null) return _store.Snapshot.Appointments.Where(a => a.DoctorUserId == _currentUser.AssignedDoctorUserId);
        return _store.Snapshot.Appointments;
    }

    private IEnumerable<NotificationMessage> VisibleNotifications()
    {
        var patient = CurrentPatient();
        if (patient is null)
        {
            return Enumerable.Empty<NotificationMessage>();
        }

        return _store.Snapshot.Notifications
            .Where(notification => notification.PatientId == patient.Id)
            .OrderByDescending(notification => notification.CreatedAt);
    }

    private IEnumerable<Prescription> VisiblePrescriptions() => FilterClinical(_store.Snapshot.Prescriptions, p => p.PatientId, p => p.DoctorUserId);
    private IEnumerable<Radiograph> VisibleRadiographs() => FilterClinical(_store.Snapshot.Radiographs, r => r.PatientId, r => r.DoctorUserId);
    private IEnumerable<TreatmentPlan> VisibleTreatments() => FilterClinical(_store.Snapshot.Treatments, t => t.PatientId, t => t.DoctorUserId);

    private IEnumerable<T> FilterClinical<T>(IEnumerable<T> source, Func<T, string> patientId, Func<T, string> doctorId)
    {
        if (IsPatient)
        {
            var patient = CurrentPatient();
            return patient is null ? Enumerable.Empty<T>() : source.Where(item => patientId(item) == patient.Id);
        }
        if (IsDoctor) return source.Where(item => doctorId(item) == _currentUser.Id);
        if (IsSecretary && _currentUser.AssignedDoctorUserId is not null) return source.Where(item => doctorId(item) == _currentUser.AssignedDoctorUserId);
        return source;
    }

    private IEnumerable<SystemLog> VisibleLogs()
    {
        if (IsAdmin) return _store.Snapshot.Logs.OrderByDescending(l => l.Timestamp);
        if (IsPatient)
        {
            var patient = CurrentPatient();
            return patient is null ? Enumerable.Empty<SystemLog>() : _store.Snapshot.Logs.Where(l => l.PatientId == patient.Id).OrderByDescending(l => l.Timestamp);
        }
        if (IsDoctor) return _store.Snapshot.Logs.Where(l => l.DoctorUserId == _currentUser.Id || l.ActorUserId == _currentUser.Id).OrderByDescending(l => l.Timestamp);
        if (IsSecretary && _currentUser.AssignedDoctorUserId is not null) return _store.Snapshot.Logs.Where(l => l.DoctorUserId == _currentUser.AssignedDoctorUserId || l.ActorUserId == _currentUser.Id).OrderByDescending(l => l.Timestamp);
        return Enumerable.Empty<SystemLog>();
    }

    private async void AddAppointment()
    {
        if (IsPatient && CurrentPatient() is null)
        {
            MessageBox.Show("Hasta hesabınız bir hasta dosyasıyla eşleşmiyor. Lütfen klinik dosyanızı kontrol edin.", "Hasta Eşleşmesi Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var item = EntityEditorForms.Appointment(_store, null, _currentUser);
        if (item is null) return;
        if (HasAppointmentConflict(item))
        {
            MessageBox.Show("Bu doktorun seçilen gün ve saatte aktif bir randevusu var. Lütfen başka bir saat seçin.", "Randevu Çakışması", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (IsPatient)
        {
            var patient = CurrentPatient();
            if (patient is null)
            {
                MessageBox.Show("Hasta hesabınız bir hasta dosyasıyla eşleşmiyor. Lütfen klinik dosyanızı kontrol edin.", "Hasta Eşleşmesi Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            item.PatientId = patient.Id;
            item.Status = AppointmentStatus.TalepEdildi;
            item.RequestedByUserId = _currentUser.Id;
            item.ApprovedByUserId = null;
            item.Notes = "Hasta portaldan talep etti";
        }

        _store.Snapshot.Appointments.Add(item);
        await _store.AddLogAsync(_currentUser, "Randevu Talebi", $"{_store.PatientName(item.PatientId)} için randevu kaydı oluşturuldu.", item.PatientId, item.DoctorUserId);
        await NotifyPatientAsync(item, "Randevu talebiniz alındı", $"{item.StartsAt:dd.MM.yyyy HH:mm} için {_store.DoctorName(item.DoctorUserId)} randevu talebiniz alındı.");
        ShowAppointments();
    }

    private async void AddPrescription()
    {
        var item = EntityEditorForms.Prescription(_store, null, _currentUser);
        if (item is null) return;
        _store.Snapshot.Prescriptions.Add(item);
        await _store.AddLogAsync(_currentUser, "Reçete Yazıldı", $"{_store.PatientName(item.PatientId)} için {item.Topic} reçetesi yazıldı.", item.PatientId, item.DoctorUserId);
        ShowPrescriptions();
    }

    private async void AddRadiograph()
    {
        var item = EntityEditorForms.Radiograph(_store, null, _currentUser);
        if (item is null) return;
        if (string.IsNullOrWhiteSpace(item.ImagePath))
        {
            item.ImagePath = MockRadiographGenerator.EnsureImage($"rontgen-{DateTime.Now:yyyyMMddHHmmss}.png", _store.Snapshot.Radiographs.Count + 10);
        }

        _store.Snapshot.Radiographs.Add(item);
        await _store.AddLogAsync(_currentUser, "Röntgen Eklendi", $"{_store.PatientName(item.PatientId)} için röntgen kaydı eklendi.", item.PatientId, item.DoctorUserId);
        ShowRadiographs();
    }

    private async void AddTreatment()
    {
        var item = EntityEditorForms.Treatment(_store, null, _currentUser);
        if (item is null) return;
        _store.Snapshot.Treatments.Add(item);
        await _store.AddLogAsync(_currentUser, "Tedavi Notu", $"{_store.PatientName(item.PatientId)} için tedavi notu eklendi.", item.PatientId, item.DoctorUserId);
        ShowTreatments();
    }

    private async Task ChangeAppointmentStatus(Appointment appointment, AppointmentStatus status)
    {
        if (status == AppointmentStatus.Onaylandi && HasAppointmentConflict(appointment))
        {
            MessageBox.Show("Bu doktorun seçilen gün ve saatte aktif bir randevusu var. Onay verilemez.", "Randevu Çakışması", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        appointment.Status = status;
        if (status is AppointmentStatus.Onaylandi or AppointmentStatus.Reddedildi)
        {
            appointment.ApprovedByUserId = _currentUser.Id;
        }

        await _store.AddLogAsync(_currentUser, "Randevu Durumu", $"{_store.PatientName(appointment.PatientId)} randevusu {StatusText(status)} olarak güncellendi.", appointment.PatientId, appointment.DoctorUserId);
        await NotifyPatientAsync(appointment, "Randevu durumu güncellendi", $"{appointment.StartsAt:dd.MM.yyyy HH:mm} tarihli {_store.DoctorName(appointment.DoctorUserId)} randevunuz {StatusText(status)} olarak güncellendi.");
        ShowAppointments();
    }

    private bool HasAppointmentConflict(Appointment appointment) =>
        _store.Snapshot.Appointments.Any(item =>
            item.Id != appointment.Id &&
            item.DoctorUserId == appointment.DoctorUserId &&
            item.StartsAt == appointment.StartsAt &&
            item.Status is AppointmentStatus.TalepEdildi or AppointmentStatus.Onaylandi or AppointmentStatus.Geldi);

    private async Task NotifyPatientAsync(Appointment appointment, string title, string body)
    {
        var patient = _store.Snapshot.Patients.FirstOrDefault(item => item.Id == appointment.PatientId);
        if (patient is null)
        {
            return;
        }

        var emailInfo = string.IsNullOrWhiteSpace(patient.Email) ? "" : $" Simüle e-posta: {patient.Email}";
        await _store.AddNotificationAsync(patient.Id, title, body + emailInfo, patient.Email);
    }

    private async Task SendPatientInfoToEmail(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.Email))
        {
            MessageBox.Show("Kayıtlı e-posta bulunamadı. Önce profil bilgilerinizden e-posta adresinizi ekleyin.", "E-posta Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var body = $"Profil özetiniz e-postanıza hazırlandı. TC: {patient.TcNo}, ad soyad: {patient.FullName}, telefon: {patient.Phone}, kan grubu: {patient.BloodType}, alerji: {patient.AllergyNotes}, kronik hastalık: {patient.ChronicDiseases}.";
        await _store.AddNotificationAsync(patient.Id, "Bilgileriniz e-postanıza gönderildi", body, patient.Email);
        MessageBox.Show($"{patient.Email} adresine simüle e-posta gönderildi ve bildirimlerinize eklendi.", "E-posta Gönderildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private Control MetricCard(string title, string value, string subtitle, Color color)
    {
        var card = ModernUi.Card();
        card.Dock = DockStyle.Fill;
        card.Controls.Add(Stack(
            ModernUi.Label(title, new Font("Segoe UI Semibold", 10F), ModernUi.Muted),
            ModernUi.Label(value, new Font("Segoe UI Semibold", 28F), color),
            ModernUi.Label(subtitle, ModernUi.SmallFont, ModernUi.Muted)));
        return card;
    }

    private Control PatientListCard(Patient patient, Action select)
    {
        var card = ModernUi.Card();
        card.Width = 305;
        card.Height = 150;
        card.Cursor = Cursors.Hand;
        card.Controls.Add(Stack(
            ModernUi.Label(patient.FullName, ModernUi.HeaderFont),
            ModernUi.Label($"{patient.Age} yaş - {patient.BloodType} - Risk: {patient.RiskLevel}", ModernUi.SmallFont, ModernUi.Muted),
            ModernUi.Label(patient.DentalHistory, ModernUi.SmallFont, ModernUi.Text)));
        Wire(card, select);
        return card;
    }

    private Control AppointmentCard(Appointment appointment, bool actions)
    {
        var card = ModernUi.Card();
        card.Width = 480;
        card.Height = actions && !IsPatient ? 340 : 285;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = actions ? 6 : 5,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        if (actions)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        }

        layout.Controls.Add(Badge(StatusText(appointment.Status), StatusColor(appointment.Status)).With(badge =>
        {
            badge.Dock = DockStyle.Fill;
            badge.Width = 0;
            badge.Margin = new Padding(0, 0, 0, 6);
        }), 0, 0);
        layout.Controls.Add(CardLabel(appointment.StartsAt.ToString("dd.MM.yyyy HH:mm"), new Font("Segoe UI Semibold", 18F), ModernUi.Text), 0, 1);
        layout.Controls.Add(CardLabel($"{_store.PatientName(appointment.PatientId)} - {_store.DoctorName(appointment.DoctorUserId)}", ModernUi.HeaderFont, ModernUi.Primary), 0, 2);
        layout.Controls.Add(CardLabel(appointment.Complaint, ModernUi.BodyFont, ModernUi.Text), 0, 3);
        layout.Controls.Add(CardLabel(appointment.Notes, ModernUi.SmallFont, ModernUi.Muted), 0, 4);

        if (actions)
        {
            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0),
                Margin = new Padding(0)
            };
            if ((CanOffice || CanClinical) && appointment.Status == AppointmentStatus.TalepEdildi)
            {
                row.Controls.Add(ActionButton("Onayla", ModernUi.Accent, async () => await ChangeAppointmentStatus(appointment, AppointmentStatus.Onaylandi)));
                row.Controls.Add(ActionButton("Reddet", ModernUi.Danger, async () => await ChangeAppointmentStatus(appointment, AppointmentStatus.Reddedildi)));
            }
            if ((CanOffice || CanClinical) && appointment.Status == AppointmentStatus.Onaylandi)
            {
                row.Controls.Add(ActionButton("Geldi", Color.FromArgb(92, 107, 192), async () => await ChangeAppointmentStatus(appointment, AppointmentStatus.Geldi)));
            }
            if (CanClinical && appointment.Status == AppointmentStatus.Geldi)
            {
                row.Controls.Add(ActionButton("Tamamla", ModernUi.Primary, async () => await ChangeAppointmentStatus(appointment, AppointmentStatus.Tamamlandi)));
            }
            if ((IsPatient || CanOffice || CanClinical) && appointment.Status is AppointmentStatus.TalepEdildi or AppointmentStatus.Onaylandi)
            {
                row.Controls.Add(ActionButton("İptal Et", Color.FromArgb(230, 236, 244), async () => await ChangeAppointmentStatus(appointment, AppointmentStatus.Iptal), ModernUi.Text));
            }
            layout.Controls.Add(row, 0, 5);
        }
        card.Controls.Add(layout);
        return card;
    }

    private Control PrescriptionCard(Prescription prescription)
    {
        var card = ModernUi.Card();
        card.Width = 470;
        card.MinimumSize = new Size(470, 420);
        card.AutoSize = true;
        card.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var stack = (FlowLayoutPanel)Stack(
            ModernUi.Label(prescription.Topic, ModernUi.HeaderFont),
            ModernUi.Label($"{_store.PatientName(prescription.PatientId)} - {_store.DoctorName(prescription.DoctorUserId)} - {prescription.Date:dd.MM.yyyy}", ModernUi.SmallFont, ModernUi.Muted),
            InfoBlock("İlaçlar", prescription.Medicines),
            InfoBlock("Otomatik Kullanım", prescription.UsageInstructions),
            InfoBlock("Doktor Notu", prescription.DoctorNote));

        stack.AutoSize = true;
        stack.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        card.Controls.Add(stack);
        return card;
    }

    private Control NotificationCard(NotificationMessage notification)
    {
        var card = ModernUi.Card();
        card.Width = 470;
        card.Height = 280;

        var emailLine = string.IsNullOrWhiteSpace(notification.EmailTo)
            ? "Arayüz içi bildirim"
            : $"Arayüz içi bildirim + simüle e-posta: {notification.EmailTo}";

        card.Controls.Add(Stack(
            Badge(notification.Read ? "Okundu" : "Yeni", notification.Read ? ModernUi.Muted : ModernUi.Accent),
            ModernUi.Label(notification.Title, ModernUi.HeaderFont),
            ModernUi.Label(notification.CreatedAt.ToString("dd.MM.yyyy HH:mm"), ModernUi.SmallFont, ModernUi.Muted),
            ModernUi.Label(notification.Body, ModernUi.BodyFont, ModernUi.Text),
            ModernUi.Label(emailLine, ModernUi.SmallFont, ModernUi.Muted)));
        return card;
    }

    private Control RadiographCard(Radiograph radiograph)
    {
        var card = ModernUi.Card();
        card.Width = 430;
        card.Height = 380;
        var picture = new PictureBox { Dock = DockStyle.Top, Height = 200, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(20, 28, 40) };
        LoadPicture(picture, radiograph);
        card.Controls.Add(Stack(
            picture,
            ModernUi.Label($"{radiograph.ToothRegion} - {radiograph.Date:dd.MM.yyyy}", ModernUi.HeaderFont),
            ModernUi.Label($"{_store.PatientName(radiograph.PatientId)} / {_store.DoctorName(radiograph.DoctorUserId)}", ModernUi.SmallFont, ModernUi.Muted),
            ModernUi.Label(radiograph.Notes, ModernUi.SmallFont, ModernUi.Text)));
        return card;
    }

    private Control TreatmentCard(TreatmentPlan treatment)
    {
        var card = ModernUi.Card();
        card.Width = 430;
        card.Height = 250;
        card.Controls.Add(Stack(
            Badge(treatment.Completed ? "Tamamlandı" : "Devam Ediyor", treatment.Completed ? ModernUi.Accent : ModernUi.Warning),
            ModernUi.Label(treatment.ProcedureName, ModernUi.HeaderFont),
            ModernUi.Label($"{_store.PatientName(treatment.PatientId)} - Diş {treatment.ToothNo} - {treatment.Date:dd.MM.yyyy}", ModernUi.SmallFont, ModernUi.Muted),
            ModernUi.Label(treatment.Description, ModernUi.BodyFont, ModernUi.Text)));
        return card;
    }

    private Control DoctorCard(UserAccount doctor)
    {
        var secretary = _store.Secretaries.FirstOrDefault(sec => sec.AssignedDoctorUserId == doctor.Id);
        var card = ModernUi.Card();
        card.Width = 500;
        card.Height = 480;
        
        var stack = (FlowLayoutPanel)Stack(
            ModernUi.Label(doctor.FullName + (doctor.Active ? "" : " (Pasif)"), new Font("Segoe UI Semibold", 18F), doctor.Active ? ModernUi.Text : ModernUi.Danger),
            ModernUi.Label($"{doctor.Specialty} - Oda {doctor.RoomName}", ModernUi.HeaderFont, ModernUi.Primary),
            ModernUi.Label(doctor.Biography, ModernUi.BodyFont, ModernUi.Text),
            InfoBlock("Sekreter", secretary is null ? "Atanmamış" : $"{secretary.FullName} / {secretary.Phone}"),
            InfoBlock("İletişim", $"{doctor.Email} / {doctor.Phone}"));
            
        stack.Dock = DockStyle.Top;
        stack.AutoSize = true;
        stack.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            
        if (IsAdmin)
        {
            var btnRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 15, 0, 0),
                Margin = new Padding(0)
            };

            var btnColor = doctor.Active ? ModernUi.Danger : ModernUi.Accent;
            var toggle = ActionButton(doctor.Active ? "Doktoru Sil (Pasifize Et)" : "Doktoru Aktifleştir", 240, btnColor);
            toggle.Margin = new Padding(0, 0, 10, 0);
            toggle.Click += async (_, _) => 
            {
                var msg = doctor.Active ? "pasifize etmek" : "aktifleştirmek";
                if (MessageBox.Show($"{doctor.FullName} adlı doktoru {msg} istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    doctor.Active = !doctor.Active;
                    var logAct = doctor.Active ? "Doktor Aktifleştirme" : "Doktor Silme";
                    var logMsg = doctor.Active ? "hesabı aktifleştirildi." : "hesabı pasifize edildi.";
                    await _store.AddLogAsync(_currentUser, logAct, $"{doctor.FullName} {logMsg}", null, doctor.Id);
                    ShowStaff();
                }
            };

            var editBtn = ActionButton("Bilgileri Düzenle", 160, ModernUi.Primary);
            editBtn.Margin = new Padding(0);
            editBtn.Click += async (_, _) =>
            {
                var (updatedDoc, secId) = EntityEditorForms.Doctor(_store, doctor);
                if (updatedDoc is not null)
                {
                    var idx = _store.Snapshot.Users.FindIndex(u => u.Id == doctor.Id);
                    if (idx >= 0)
                    {
                        _store.Snapshot.Users[idx] = updatedDoc;
                        
                        foreach (var s in _store.Snapshot.Users.Where(u => u.Role == UserRole.Sekreter && u.AssignedDoctorUserId == updatedDoc.Id))
                        {
                            s.AssignedDoctorUserId = null;
                        }

                        if (!string.IsNullOrWhiteSpace(secId))
                        {
                            var newSec = _store.Snapshot.Users.FirstOrDefault(u => u.Id == secId);
                            if (newSec is not null)
                            {
                                newSec.AssignedDoctorUserId = updatedDoc.Id;
                            }
                        }

                        await _store.AddLogAsync(_currentUser, "Doktor Düzenleme", $"{updatedDoc.FullName} bilgileri güncellendi.", null, updatedDoc.Id);
                        ShowStaff();
                    }
                }
            };

            btnRow.Controls.Add(toggle);
            btnRow.Controls.Add(editBtn);
            stack.Controls.Add(btnRow);
        }
            
        card.Controls.Add(stack);
        return card;
    }

    private Control LogCard(SystemLog log)
    {
        var card = ModernUi.Card();
        card.Width = 440;
        card.Height = 185;
        card.Controls.Add(Stack(
            Badge(log.ActionType, ModernUi.Primary),
            ModernUi.Label($"{log.ActorName} ({log.ActorRole})", new Font("Segoe UI Semibold", 10F), ModernUi.Text),
            ModernUi.Label(log.Timestamp.ToString("dd.MM.yyyy HH:mm"), ModernUi.SmallFont, ModernUi.Muted),
            ModernUi.Label(log.Description, ModernUi.BodyFont, ModernUi.Text)));
        return card;
    }

    private Control InfoCard(string title, string value, Color color)
    {
        var card = ModernUi.Card();
        card.Width = 360;
        card.Height = 180;
        card.Controls.Add(Stack(
            ModernUi.Label(title, ModernUi.HeaderFont, color),
            ModernUi.Label(string.IsNullOrWhiteSpace(value) ? "-" : value, ModernUi.BodyFont, ModernUi.Text)));
        return card;
    }

    private Control InfoBlock(string title, string value)
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitle = ModernUi.Label(title, ModernUi.SmallFont, ModernUi.Muted);
        lblTitle.Margin = new Padding(0, 4, 0, 0);
        
        var lblValue = ModernUi.Label(string.IsNullOrWhiteSpace(value) ? "-" : value, ModernUi.BodyFont, ModernUi.Text);
        lblValue.Margin = new Padding(0, 0, 0, 4);
        lblValue.MaximumSize = new Size(400, 0);

        panel.Controls.Add(lblTitle, 0, 0);
        panel.Controls.Add(lblValue, 0, 1);
        return panel;
    }

    private Label ProfileLine(string title, string value, int titleSize = 10) =>
        ModernUi.Label($"{title}: {value}", new Font("Segoe UI Semibold", titleSize), ModernUi.Text);

    private static Label CardLabel(string text, Font font, Color color) =>
        ModernUi.Label(text, font, color).With(label =>
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Margin = new Padding(0, 0, 0, 6);
        });

    private static Label Badge(string text, Color color) => new()
    {
        Text = text,
        AutoSize = false,
        Width = 150,
        Height = 30,
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.FromArgb(235, 241, 248),
        ForeColor = color,
        Font = new Font("Segoe UI Semibold", 9.5F),
        Margin = new Padding(0, 0, 0, 8)
    };

    private Button ActionButton(string text, Color backColor, Func<Task> action, Color? foreColor = null)
    {
        var button = ModernUi.FlatButton(text, backColor, foreColor ?? Color.White);
        button.Width = 112;
        button.Height = 38;
        button.Click += async (_, _) => await action();
        return button;
    }

    private Button PrimaryAction(string text, int width)
    {
        var button = ModernUi.PrimaryButton(text);
        button.Width = width;
        return button;
    }

    private Button ActionButton(string text, int width, Color backColor)
    {
        var button = ModernUi.FlatButton(text, backColor, Color.White);
        button.Width = width;
        return button;
    }

    private Control Section(string title, Control content)
    {
        var card = ModernUi.Card();
        card.Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(ModernUi.Label(title, ModernUi.HeaderFont), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private TabPage Tab(string title, Control content)
    {
        var tab = new TabPage(title) { BackColor = ModernUi.Background, Padding = new Padding(8) };
        tab.Controls.Add(content);
        return tab;
    }

    private FlowLayoutPanel Flow(IEnumerable<Control> controls)
    {
        var panel = FlowPanel();
        foreach (var control in controls)
        {
            panel.Controls.Add(control);
        }
        return panel;
    }


    private FlowLayoutPanel FlowPanel() => new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(4)
    };

    private Control Stack(params Control[] controls)
    {
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        foreach (var control in controls)
        {
            control.Dock = DockStyle.Top;
            stack.Controls.Add(control);
        }
        return stack;
    }

    private Control Avatar(string fullName, int size)
    {
        return new Label
        {
            Text = Initials(fullName),
            Width = size,
            Height = size,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(226, 239, 255),
            ForeColor = ModernUi.Primary,
            Font = new Font("Segoe UI Semibold", size / 3F),
            Margin = new Padding(0, 0, 18, 0)
        };
    }

    private static string Initials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static Color StatusColor(AppointmentStatus status) => status switch
    {
        AppointmentStatus.TalepEdildi => ModernUi.Warning,
        AppointmentStatus.Onaylandi => ModernUi.Accent,
        AppointmentStatus.Geldi => Color.FromArgb(92, 107, 192),
        AppointmentStatus.Tamamlandi => ModernUi.Primary,
        AppointmentStatus.Reddedildi => ModernUi.Danger,
        AppointmentStatus.Iptal => ModernUi.Muted,
        _ => ModernUi.Text
    };

    private static string StatusText(AppointmentStatus status) => status switch
    {
        AppointmentStatus.TalepEdildi => "Onay Bekliyor",
        AppointmentStatus.Onaylandi => "Onaylandı",
        AppointmentStatus.Geldi => "Hasta Geldi",
        AppointmentStatus.Tamamlandi => "Tamamlandı",
        AppointmentStatus.Reddedildi => "Reddedildi",
        AppointmentStatus.Iptal => "İptal",
        _ => status.ToString()
    };

    private static void LoadPicture(PictureBox picture, Radiograph radiograph)
    {
        if (!string.IsNullOrWhiteSpace(radiograph.ImageContentBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(radiograph.ImageContentBase64);
                using var memory = new MemoryStream(bytes);
                using var loaded = Image.FromStream(memory);
                picture.Image = new Bitmap(loaded);
                return;
            }
            catch
            {
                picture.Image = null;
            }
        }

        var path = radiograph.ImagePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            picture.Image = null;
            return;
        }

        using var stream = File.OpenRead(path);
        using var pathImage = Image.FromStream(stream);
        picture.Image = new Bitmap(pathImage);
    }

    private static void Wire(Control control, Action action)
    {
        control.Click += (_, _) => action();
        foreach (Control child in control.Controls)
        {
            Wire(child, action);
        }
    }
}
