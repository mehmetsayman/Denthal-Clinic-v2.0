using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Forms;

public static class EntityEditorForms
{
    public static Appointment? Appointment(ClinicDataStore store, Appointment? source, UserAccount currentUser)
    {
        var entity = source is null ? new Appointment { RequestedByUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Randevu Bilgisi", 560, 560);
        var currentPatient = currentUser.Role == UserRole.Hasta ? ResolvePatientForUser(store, currentUser) : null;
        if (currentPatient is not null)
        {
            entity.PatientId = currentPatient.Id;
        }

        IEnumerable<Patient> patientSource = currentPatient is null ? store.Snapshot.Patients : new[] { currentPatient };
        var patient = Lookup(patientSource, item => item.FullName, item => item.Id);
        var doctor = Lookup(store.Doctors.ToList(), item => item.FullName, item => item.Id);
        var patientBox = Combo(patient, entity.PatientId);
        var doctorBox = Combo(doctor, entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm", Value = entity.StartsAt, Font = ModernUi.BodyFont };
        var duration = Number(entity.DurationMinutes, 10, 240);
        var status = Combo(Enum.GetNames<AppointmentStatus>().Select(x => new LookupItem(x, x)).ToList(), entity.Status.ToString());
        var complaint = Text(entity.Complaint, "Sikayet");
        var notes = Text(entity.Notes, "Notlar", true);

        if (currentUser.Role == UserRole.Hasta && currentPatient is not null)
        {
            patientBox.SelectedValue = currentPatient.Id;
            patientBox.Enabled = false;
            status.SelectedValue = AppointmentStatus.TalepEdildi.ToString();
            status.Enabled = false;
        }

        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih/Saat", date), ("Sure (dk)", duration), ("Durum", status), ("Sikayet", complaint), ("Not", notes));
        return Show(form, () =>
        {
            entity.PatientId = currentPatient?.Id ?? Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.RequestedByUserId = string.IsNullOrWhiteSpace(entity.RequestedByUserId) ? currentUser.Id : entity.RequestedByUserId;
            entity.StartsAt = date.Value;
            entity.DurationMinutes = (int)duration.Value;
            entity.Status = Enum.TryParse<AppointmentStatus>(Value(status), out var parsed) ? parsed : AppointmentStatus.TalepEdildi;
            entity.Complaint = complaint.Text.Trim();
            entity.Notes = notes.Text.Trim();
            return entity;
        });
    }

    private static Patient? ResolvePatientForUser(ClinicDataStore store, UserAccount user)
    {
        var patient = !string.IsNullOrWhiteSpace(user.LinkedPatientId)
            ? store.Snapshot.Patients.FirstOrDefault(item => item.Id == user.LinkedPatientId)
            : null;

        patient ??= store.Snapshot.Patients.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(user.UserName) &&
            item.TcNo.Equals(user.UserName, StringComparison.OrdinalIgnoreCase));

        patient ??= store.Snapshot.Patients.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(user.Email) &&
            item.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

        patient ??= store.Snapshot.Patients.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(user.FullName) &&
            item.FullName.Equals(user.FullName, StringComparison.OrdinalIgnoreCase));

        if (patient is not null)
        {
            user.LinkedPatientId = patient.Id;
            var storedUser = store.Snapshot.Users.FirstOrDefault(item => item.Id == user.Id);
            if (storedUser is not null)
            {
                storedUser.LinkedPatientId = patient.Id;
            }
        }

        return patient;
    }

    public static Prescription? Prescription(ClinicDataStore store, Prescription? source, UserAccount currentUser)
    {
        var entity = source is null ? new Prescription { DoctorUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Akıllı Reçete", 700, 720);
        var patientBox = Combo(Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id), entity.PatientId);
        var doctorBox = Combo(Lookup(store.Doctors.ToList(), item => item.FullName, item => item.Id), entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = entity.Date, Font = ModernUi.BodyFont };
        var topicBox = Combo(
            new[] { "Kanal Tedavisi", "Dolgu Sonrası", "Diş Eti Tedavisi", "Cerrahi Hazırlık", "Ortodontik Hazırlık", "İmplant Planlama", "Rutin Kontrol" }
                .Select(topic => new LookupItem(topic, topic)).ToList(),
            entity.Topic);
        var diagnosis = Text(entity.Diagnosis, "Tanı / klinik konu");
        var medicines = new CheckedListBox
        {
            CheckOnClick = true,
            Font = ModernUi.BodyFont,
            Height = 180,
            DisplayMember = nameof(MedicationTemplate.Name),
            ValueMember = nameof(MedicationTemplate.Id)
        };
        foreach (var medication in store.Snapshot.Medications)
        {
            var index = medicines.Items.Add(medication);
            if (entity.MedicationIds.Contains(medication.Id))
            {
                medicines.SetItemChecked(index, true);
            }
        }

        var usage = Text(entity.UsageInstructions, "Secilen ilaclarin talimatlari otomatik gelir", true);
        usage.Height = 120;
        usage.ReadOnly = true;
        var note = Text(entity.DoctorNote, "Doktor notu", true);
        note.Height = 84;

        void RefreshUsage()
        {
            var selected = medicines.CheckedItems.Cast<MedicationTemplate>().ToList();
            usage.Text = string.Join(Environment.NewLine, selected.Select(med => $"{med.Name}: {med.DefaultUsage}"));
        }

        medicines.ItemCheck += (_, _) => form.BeginInvoke(RefreshUsage);
        topicBox.SelectedIndexChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(diagnosis.Text))
            {
                diagnosis.Text = Value(topicBox);
            }
        };
        RefreshUsage();
        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih", date), ("Reçete Konusu", topicBox), ("Tanı", diagnosis), ("İlaç Seçimi", medicines), ("Otomatik Kullanım Talimati", usage), ("Doktor Notu", note));
        return Show(form, () =>
        {
            var selected = medicines.CheckedItems.Cast<MedicationTemplate>().ToList();
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.Date = date.Value.Date;
            entity.Topic = Value(topicBox);
            entity.Diagnosis = diagnosis.Text.Trim();
            entity.MedicationIds = selected.Select(med => med.Id).ToList();
            entity.Medicines = string.Join("; ", selected.Select(med => med.Name));
            entity.UsageInstructions = usage.Text.Trim();
            entity.DoctorNote = note.Text.Trim();
            return entity;
        });
    }

    public static Radiograph? Radiograph(ClinicDataStore store, Radiograph? source, UserAccount currentUser)
    {
        var entity = source is null ? new Radiograph { DoctorUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Diş Röntgeni", 560, 560);
        var patientBox = Combo(Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id), entity.PatientId);
        var doctorBox = Combo(Lookup(store.Doctors.ToList(), item => $"{item.FullName} - {item.Specialty}", item => item.Id), entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = entity.Date, Font = ModernUi.BodyFont };
        var region = Text(entity.ToothRegion, "Diş bölgesi");
        var imagePath = Text(entity.ImagePath, "Görsel dosya yolu");
        var choose = ModernUi.FlatButton("Görsel Seç", Color.FromArgb(230, 236, 244), ModernUi.Text);
        choose.Click += (_, _) =>
        {
            using var file = new OpenFileDialog
            {
                Filter = "Görsel Dosyaları|*.png;*.jpg;*.jpeg;*.bmp|Tüm Dosyalar|*.*",
                Title = "Röntgen görseli seç"
            };
            if (file.ShowDialog(form) == DialogResult.OK)
            {
                imagePath.Text = file.FileName;
            }
        };
        var notes = Text(entity.Notes, "Röntgen notu", true);
        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih", date), ("Bölge", region), ("Görsel", imagePath), ("", choose), ("Not", notes));
        return Show(form, () =>
        {
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.Date = date.Value.Date;
            entity.ToothRegion = region.Text.Trim();
            if (!entity.ImagePath.Equals(imagePath.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                entity.ImageFileName = "";
                entity.ImageMimeType = "";
                entity.ImageContentBase64 = "";
            }

            entity.ImagePath = imagePath.Text.Trim();
            entity.Notes = notes.Text.Trim();
            return entity;
        });
    }

    public static TreatmentPlan? Treatment(ClinicDataStore store, TreatmentPlan? source, UserAccount currentUser)
    {
        var entity = source is null ? new TreatmentPlan { DoctorUserId = currentUser.Id } : Clone(source);
        using var form = Dialog("Tedavi Plani", 560, 620);
        var patientBox = Combo(Lookup(store.Snapshot.Patients, item => item.FullName, item => item.Id), entity.PatientId);
        var doctorBox = Combo(Lookup(store.Doctors.ToList(), item => item.FullName, item => item.Id), entity.DoctorUserId);
        var date = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = entity.Date, Font = ModernUi.BodyFont };
        var toothNo = Text(entity.ToothNo, "Diş no");
        var procedure = Text(entity.ProcedureName, "İşlem");
        var description = Text(entity.Description, "Açıklama", true);
        var completed = new CheckBox { Text = "Tedavi tamamlandi", Checked = entity.Completed, AutoSize = true, ForeColor = ModernUi.Text };
        AddRows(form, ("Hasta", patientBox), ("Doktor", doctorBox), ("Tarih", date), ("Diş No", toothNo), ("İşlem", procedure), ("Açıklama", description), ("Durum", completed));
        return Show(form, () =>
        {
            entity.PatientId = Value(patientBox);
            entity.DoctorUserId = Value(doctorBox);
            entity.Date = date.Value.Date;
            entity.ToothNo = toothNo.Text.Trim();
            entity.ProcedureName = procedure.Text.Trim();
            entity.Description = description.Text.Trim();
            entity.Completed = completed.Checked;
            return entity;
        });
    }

    private static Form Dialog(string title, int width, int height)
    {
        return new Form
        {
            Text = title,
            Size = new Size(width, height),
            MinimumSize = new Size(width, height),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ModernUi.Background,
            Font = ModernUi.BodyFont
        };
    }

    private static void AddDialogButtons(Form form)
    {
        var buttons = new FlowLayoutPanel 
        { 
            Dock = DockStyle.Bottom, 
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Color.FromArgb(235, 241, 248)
        };
        var save = ModernUi.PrimaryButton("Kaydet");
        var cancel = ModernUi.FlatButton("Vazgeç", Color.FromArgb(230, 236, 244), ModernUi.Text);
        save.Width = cancel.Width = 120;
        save.Click += (_, _) => form.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        form.Controls.Add(buttons);
    }

    private static void AddRows(Form form, params (string Label, Control Control)[] rows)
    {
        var buttons = new FlowLayoutPanel 
        { 
            Dock = DockStyle.Bottom, 
            Height = 60, // Compact height
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Color.FromArgb(235, 241, 248)
        };
        var save = ModernUi.PrimaryButton("Kaydet");
        var cancel = ModernUi.FlatButton("Vazgeç", Color.FromArgb(230, 236, 244), ModernUi.Text);
        save.Width = cancel.Width = 120;
        save.Click += (_, _) => form.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        form.Controls.Add(buttons);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = new Padding(16)
        };
        form.Controls.Add(panel);
        panel.BringToFront();

        panel.Controls.Add(ModernUi.Label(form.Text, ModernUi.HeaderFont));
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Label))
            {
                panel.Controls.Add(ModernUi.Label(row.Label, new Font("Segoe UI Semibold", 9.2F), ModernUi.Text));
            }

            row.Control.Dock = DockStyle.Top;
            row.Control.Margin = new Padding(0, 2, 0, 10);
            panel.Controls.Add(row.Control);
        }

        form.AcceptButton = save;
        form.CancelButton = cancel;
    }

    private static T? Show<T>(Form form, Func<T> factory) where T : class
    {
        return form.ShowDialog() == DialogResult.OK ? factory() : null;
    }

    private static TextBox Text(string value, string placeholder, bool multiline = false)
    {
        var box = ModernUi.TextBox(placeholder);
        box.Text = value;
        box.Multiline = multiline;
        if (multiline)
        {
            box.Height = 78;
            box.ScrollBars = ScrollBars.Vertical;
        }

        return box;
    }

    private static Control Wrap(string label, Control control)
    {
        var panel = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 5, 0, 10) };
        var lbl = ModernUi.Label(label, ModernUi.BodyFont, ModernUi.Muted);
        lbl.AutoSize = true;
        lbl.Dock = DockStyle.Top;
        lbl.Padding = new Padding(0, 0, 0, 5);
        control.Dock = DockStyle.Top;
        panel.Controls.Add(control);
        panel.Controls.Add(lbl);
        return panel;
    }

    private static NumericUpDown Number(decimal value, decimal min, decimal max)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Min(Math.Max(value, min), max),
            DecimalPlaces = max > 500 ? 2 : 0,
            Font = ModernUi.BodyFont,
            Height = 32
        };
    }

    private static ComboBox Combo(List<LookupItem> items, string selectedValue)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = ModernUi.BodyFont,
            DisplayMember = nameof(LookupItem.Text),
            ValueMember = nameof(LookupItem.Value),
            DataSource = items
        };

        if (!string.IsNullOrWhiteSpace(selectedValue))
        {
            combo.SelectedValue = selectedValue;
        }

        return combo;
    }

    private static List<LookupItem> Lookup<T>(IEnumerable<T> source, Func<T, string> text, Func<T, string> value) =>
        source.Select(item => new LookupItem(text(item), value(item))).ToList();

    private static string Value(ComboBox combo) => combo.SelectedValue?.ToString() ?? "";

    private sealed record LookupItem(string Text, string Value);

    private static Appointment Clone(Appointment item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        RequestedByUserId = item.RequestedByUserId,
        ApprovedByUserId = item.ApprovedByUserId,
        RequestedAt = item.RequestedAt,
        StartsAt = item.StartsAt,
        DurationMinutes = item.DurationMinutes,
        Status = item.Status,
        Complaint = item.Complaint,
        Notes = item.Notes
    };

    private static Prescription Clone(Prescription item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        Date = item.Date,
        Topic = item.Topic,
        MedicationIds = [..item.MedicationIds],
        Diagnosis = item.Diagnosis,
        Medicines = item.Medicines,
        UsageInstructions = item.UsageInstructions,
        DoctorNote = item.DoctorNote
    };

    private static Radiograph Clone(Radiograph item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        Date = item.Date,
        ToothRegion = item.ToothRegion,
        ImagePath = item.ImagePath,
        ImageFileName = item.ImageFileName,
        ImageMimeType = item.ImageMimeType,
        ImageContentBase64 = item.ImageContentBase64,
        Notes = item.Notes
    };

    private static TreatmentPlan Clone(TreatmentPlan item) => new()
    {
        Id = item.Id,
        PatientId = item.PatientId,
        DoctorUserId = item.DoctorUserId,
        Date = item.Date,
        ToothNo = item.ToothNo,
        ProcedureName = item.ProcedureName,
        Description = item.Description,
        Completed = item.Completed
    };

    public static Patient? Patient(Patient? source)
    {
        var entity = source is null ? new Patient() : Clone(source);
        using var form = Dialog("Hasta Bilgisi", 560, 640);
        
        var tcBox = Text(entity.TcNo, "");
        var nameBox = Text(entity.FullName, "");
        var genderBox = Combo(Enum.GetNames<Gender>().Select(x => new LookupItem(x, x)).ToList(), entity.Gender.ToString());
        var phoneBox = Text(entity.Phone, "");
        var emailBox = Text(entity.Email, "");
        var dateBox = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy", Value = entity.BirthDate, Font = ModernUi.BodyFont };
        var bloodBox = Text(entity.BloodType, "");
        
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) };
        for (var i = 0; i < 2; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
        layout.Controls.Add(Wrap("TC Kimlik No", tcBox), 0, 0); layout.Controls.Add(Wrap("Ad Soyad", nameBox), 1, 0);
        layout.Controls.Add(Wrap("Cinsiyet", genderBox), 0, 1); layout.Controls.Add(Wrap("Telefon", phoneBox), 1, 1);
        layout.Controls.Add(Wrap("E-Posta", emailBox), 0, 2); layout.Controls.Add(Wrap("Doğum Tarihi", dateBox), 1, 2);
        layout.Controls.Add(Wrap("Kan Grubu", bloodBox), 0, 3);

        form.Controls.Add(layout);
        AddDialogButtons(form);

        if (form.ShowDialog() == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("Ad soyad zorunludur.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return null; }
            entity.TcNo = tcBox.Text;
            entity.FullName = nameBox.Text;
            entity.Gender = Enum.Parse<Gender>(genderBox.SelectedValue?.ToString() ?? "Belirtilmedi");
            entity.Phone = phoneBox.Text;
            entity.Email = emailBox.Text;
            entity.BirthDate = dateBox.Value;
            entity.BloodType = bloodBox.Text;
            return entity;
        }
        return null;
    }

    public static (UserAccount? Doctor, string? SecretaryId) Doctor(ClinicDataStore store, UserAccount? source)
    {
        var entity = source is null ? new UserAccount { Role = UserRole.Doktor } : Clone(source);
        using var form = Dialog("Doktor Bilgisi", 560, 640);
        
        var nameBox = Text(entity.FullName, "");
        var specBox = Text(entity.Specialty, "");
        var emailBox = Text(entity.Email, "");
        var passBox = Text(entity.Password, "");
        var phoneBox = Text(entity.Phone, "");
        var roomBox = Text(entity.RoomName, "");

        var secretaries = store.Snapshot.Users.Where(u => u.Role == UserRole.Sekreter).ToList();
        var secretaryOptions = new List<LookupItem> { new LookupItem("Sekreter Seçiniz...", "") };
        secretaryOptions.AddRange(secretaries.Select(s => new LookupItem(s.FullName, s.Id)));

        var currentSecretary = source != null ? store.Snapshot.Users.FirstOrDefault(u => u.Role == UserRole.Sekreter && u.AssignedDoctorUserId == source.Id) : null;
        var secretaryBox = Combo(secretaryOptions, currentSecretary?.Id ?? "");
        
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) };
        for (var i = 0; i < 2; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
        layout.Controls.Add(Wrap("Ad Soyad", nameBox), 0, 0); layout.Controls.Add(Wrap("Uzmanlik Alani", specBox), 1, 0);
        layout.Controls.Add(Wrap("E-Posta (Giriş ID)", emailBox), 0, 1); layout.Controls.Add(Wrap("Şifre", passBox), 1, 1);
        layout.Controls.Add(Wrap("Telefon", phoneBox), 0, 2); layout.Controls.Add(Wrap("Oda Ismi", roomBox), 1, 2);
        layout.Controls.Add(Wrap("Atanacak Sekreter", secretaryBox), 0, 3);

        form.Controls.Add(layout);
        AddDialogButtons(form);

        if (form.ShowDialog() == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text)) { MessageBox.Show("Ad soyad zorunludur.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return (null, null); }
            if (string.IsNullOrWhiteSpace(emailBox.Text)) { MessageBox.Show("E-posta zorunludur.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return (null, null); }
            
            entity.FullName = nameBox.Text;
            entity.Specialty = specBox.Text;
            entity.Email = emailBox.Text;
            entity.UserName = emailBox.Text.Trim();
            entity.Password = passBox.Text;
            entity.Phone = phoneBox.Text;
            entity.RoomName = roomBox.Text;
            return (entity, Value(secretaryBox));
        }
        return (null, null);
    }

    private static Patient Clone(Patient item) => new()
    {
        Id = item.Id,
        TcNo = item.TcNo,
        FullName = item.FullName,
        Gender = item.Gender,
        BirthDate = item.BirthDate,
        Phone = item.Phone,
        Email = item.Email,
        Address = item.Address,
        BloodType = item.BloodType,
        HeightCm = item.HeightCm,
        WeightKg = item.WeightKg,
        AllergyNotes = item.AllergyNotes,
        ChronicDiseases = item.ChronicDiseases,
        CurrentMedications = item.CurrentMedications,
        SmokingStatus = item.SmokingStatus,
        EmergencyContactName = item.EmergencyContactName,
        EmergencyContactPhone = item.EmergencyContactPhone,
        DentalHistory = item.DentalHistory,
        RiskLevel = item.RiskLevel,
        CreatedAt = item.CreatedAt
    };

    private static UserAccount Clone(UserAccount item) => new()
    {
        Id = item.Id,
        UserName = item.UserName,
        Password = item.Password,
        FullName = item.FullName,
        Role = item.Role,
        Specialty = item.Specialty,
        Email = item.Email,
        Phone = item.Phone,
        Biography = item.Biography,
        RoomName = item.RoomName,
        AssignedDoctorUserId = item.AssignedDoctorUserId,
        LinkedPatientId = item.LinkedPatientId,
        Active = item.Active
    };
}
