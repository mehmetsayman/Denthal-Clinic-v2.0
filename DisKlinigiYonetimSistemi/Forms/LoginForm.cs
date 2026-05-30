using System.Drawing.Drawing2D;
using System.ComponentModel;
using DisKlinigiYonetimSistemi.Controls;
using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Forms;

public sealed class LoginForm : Form
{
    private const string BrandName = "\u00C7CETY";
    private const string ClinicName = "Diş Kliniği";
    private readonly ClinicDataStore _store;
    private readonly TextBox _userNameBox = InputTextBox("TC veya kullanıcı adı");
    private readonly TextBox _passwordBox = InputTextBox("Şifre", true);
    private readonly CheckBox _showPassword = new()
    {
        Text = "Şifreyi göster",
        AutoSize = true,
        ForeColor = Color.FromArgb(226, 235, 246),
        BackColor = Color.Transparent,
        Font = new Font("Segoe UI", 10F)
    };
    private readonly Label _message = TextLabel("", new Font("Segoe UI Semibold", 9.5F), Color.FromArgb(255, 184, 184));

    public LoginForm(ClinicDataStore store)
    {
        _store = store;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(16, 28, 46);
        Text = $"{BrandName} Diş Kliniği - Giriş";
        MinimumSize = new Size(1280, 780);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        Font = ModernUi.BodyFont;
        Opacity = 0; // For fade-in
        Shown += (_, _) =>
        {
            var animator = new Animator(250);
            animator.Start(t => Opacity = t);
        };
        BuildUi();
        this.EnableDoubleBuffering();
    }

    private void BuildUi()
    {
        var surface = new LoginSurface
        {
            Dock = DockStyle.Fill,
            BackgroundPath = Path.Combine(AppContext.BaseDirectory, "Assets", "login-clinic-hero.png")
        };
        Controls.Add(surface);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(84, 72, 84, 72),
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        surface.Controls.Add(root);

        root.Controls.Add(BuildBrandPanel(), 0, 0);
        root.Controls.Add(BuildLoginShell(), 1, 0);
    }

    private Control BuildBrandPanel()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 44, 40, 40)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 460));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 70));

        var brand = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        brand.Controls.Add(TextLabel(BrandName, new Font("Segoe UI Semibold", 68F), Color.White).With(label =>
        {
            label.Margin = new Padding(0, 0, 0, 4);
        }));
        brand.Controls.Add(TextLabel(ClinicName, new Font("Segoe UI Semibold", 38F), Color.FromArgb(190, 245, 235)).With(label =>
        {
            label.Margin = new Padding(0, 0, 0, 10);
        }));
        brand.Controls.Add(TextLabel("Akıllı diş kliniği yönetim sistemi", new Font("Segoe UI", 19F), Color.FromArgb(236, 243, 250)).With(label =>
        {
            label.Margin = new Padding(0, 0, 0, 10);
        }));
        brand.Controls.Add(TextLabel("Hasta portalı, doktor dosyaları, sekreter takvimi ve admin logları tek modern sistemde.", new Font("Segoe UI", 11.5F), Color.FromArgb(212, 225, 238)).With(label =>
        {
            label.MaximumSize = new Size(620, 0);
            label.Margin = new Padding(0, 0, 0, 18);
        }));

        var chips = new FlowLayoutPanel
        {
            AutoSize = true,
            Height = 50,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        chips.Controls.Add(InfoChip("12 hasta"));
        chips.Controls.Add(InfoChip("4 doktor"));
        chips.Controls.Add(InfoChip("4 sekreter"));
        chips.Controls.Add(InfoChip("50+ log"));
        brand.Controls.Add(chips);

        outer.Controls.Add(brand, 0, 1);
        return outer;
    }

    private Control BuildLoginShell()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 600));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 820));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 36, 58),
            Padding = new Padding(52, 48, 52, 44),
            Margin = new Padding(0)
        };
        shell.Controls.Add(card, 1, 1);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 14,
            BackColor = Color.Transparent
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(stack);

        var logo = new ToothLogo { Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 0, 8) };
        stack.Controls.Add(logo, 0, 0);
        stack.Controls.Add(Centered("Klinik Girişi", new Font("Segoe UI Semibold", 30F), Color.White), 0, 1);
        stack.Controls.Add(Centered("Rolüne uygun panele güvenli geçiş yap", new Font("Segoe UI", 11F), Color.FromArgb(195, 212, 232)), 0, 2);
        stack.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 3);
        stack.Controls.Add(TextLabel("TC / Kullanıcı Adı", new Font("Segoe UI Semibold", 9.8F), Color.White), 0, 4);
        stack.Controls.Add(InputShell(_userNameBox), 0, 5);
        stack.Controls.Add(TextLabel("Şifre", new Font("Segoe UI Semibold", 9.8F), Color.White), 0, 6);
        stack.Controls.Add(InputShell(_passwordBox), 0, 7);
        stack.Controls.Add(_showPassword, 0, 8);

        var loginButton = new Button
        {
            Text = "Sisteme Gir",
            Dock = DockStyle.Fill,
            Height = 52,
            BackColor = ModernUi.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 12F),
            Cursor = Cursors.Hand
        };
        loginButton.FlatAppearance.BorderSize = 0;
        loginButton.Click += async (_, _) => await TryLogin();
        stack.Controls.Add(loginButton, 0, 9);

        var registerButton = new Button
        {
            Text = "Yeni Hasta Hesabı Oluştur",
            Dock = DockStyle.Fill,
            Height = 42,
            BackColor = Color.FromArgb(45, 61, 94),
            ForeColor = Color.FromArgb(224, 235, 248),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10.5F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 8, 0, 4)
        };
        registerButton.FlatAppearance.BorderSize = 0;
        registerButton.Click += (_, _) => RegisterPatient();
        stack.Controls.Add(registerButton, 0, 10);

        stack.Controls.Add(RoleShortcuts(), 0, 11);
        stack.Controls.Add(_message.With(label =>
        {
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
        }), 0, 12);

        var footer = Centered("Demo şifre: 123456   |   v4.0", ModernUi.SmallFont, Color.FromArgb(168, 185, 208));
        stack.Controls.Add(footer, 0, 13);

        AcceptButton = loginButton;
        _showPassword.CheckedChanged += (_, _) => _passwordBox.UseSystemPasswordChar = !_showPassword.Checked;
        return shell;
    }

    private FlowLayoutPanel RoleShortcuts()
    {
        var roles = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        roles.Controls.Add(RoleButton("Admin", "admin"));
        roles.Controls.Add(RoleButton("Doktor", "doktor"));
        roles.Controls.Add(RoleButton("Sekreter", "sekreter"));
        roles.Controls.Add(RoleButton("Hasta", "hasta"));
        return roles;
    }

    private Button RoleButton(string text, string userName)
    {
        var button = new Button
        {
            Text = text,
            Width = 94,
            Height = 36,
            BackColor = Color.FromArgb(45, 61, 94),
            ForeColor = Color.FromArgb(224, 235, 248),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.2F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) =>
        {
            _userNameBox.Text = userName;
            _passwordBox.Text = "123456";
            _message.Text = "";
        };
        return button;
    }

    private static Control InputShell(TextBox textBox)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 50,
            BackColor = Color.White,
            Padding = new Padding(14, 12, 14, 8),
            Margin = new Padding(0, 0, 0, 8)
        };
        textBox.Dock = DockStyle.Fill;
        textBox.BorderStyle = BorderStyle.None;
        textBox.Margin = new Padding(0);
        panel.Controls.Add(textBox);
        return panel;
    }

    private static TextBox InputTextBox(string placeholder, bool password = false) => new()
    {
        PlaceholderText = placeholder,
        UseSystemPasswordChar = password,
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = ModernUi.Text,
        Font = new Font("Segoe UI", 12F)
    };

    private static Label TextLabel(string text, Font font, Color color) => new()
    {
        Text = text,
        Font = font,
        ForeColor = color,
        BackColor = Color.Transparent,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 6),
        UseCompatibleTextRendering = true
    };

    private static Label Centered(string text, Font font, Color color) =>
        TextLabel(text, font, color).With(label =>
        {
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
        });

    private static Label InfoChip(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Width = 105,
        Height = 34,
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.FromArgb(36, 245, 255, 255),
        ForeColor = Color.White,
        Font = new Font("Segoe UI Semibold", 9F),
        Margin = new Padding(0, 0, 10, 0)
    };

    private async Task TryLogin()
    {
        var user = _store.Authenticate(_userNameBox.Text, _passwordBox.Text);
        if (user is null)
        {
            _message.Text = "Kullanıcı adı veya şifre hatalı.";
            return;
        }

        // Fire and forget to avoid blocking the UI
        _ = _store.AddLogAsync(user, "Giriş", $"{user.FullName} sisteme giriş yaptı.");
        
        using var dashboard = new MainForm(_store, user);
        dashboard.ShowDialog(this);
        _passwordBox.Clear();
    }

    private async void RegisterPatient()
    {
        using var dialog = PatientEditorForm.Create(_store, null, true);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Patient is null)
        {
            return;
        }

        var patient = dialog.Patient;
        _store.Snapshot.Patients.Add(patient);
        var user = new UserAccount
        {
            UserName = patient.TcNo,
            Password = dialog.CreatedPassword,
            FullName = patient.FullName,
            Role = UserRole.Hasta,
            Email = patient.Email,
            Phone = patient.Phone,
            LinkedPatientId = patient.Id
        };
        _store.Snapshot.Users.Add(user);
        await _store.AddLogAsync(user, "Hasta Kaydı", $"{patient.FullName} hasta portalı üzerinden kayıt oldu.", patient.Id);
        await _store.AddNotificationAsync(patient.Id, "Hasta hesabınız oluşturuldu", $"Giriş için TC kimlik numaranızı ({patient.TcNo}) ve belirlediğiniz şifreyi kullanabilirsiniz.", patient.Email);
        _userNameBox.Text = patient.TcNo;
        _passwordBox.Text = dialog.CreatedPassword;
        _message.ForeColor = ModernUi.Accent;
        _message.Text = $"Kayıt tamamlandı. TC kimlik numaranızla giriş yapabilirsiniz.";
    }

    private sealed class LoginSurface : Panel
    {
        private Image? _background;
        private string _backgroundPath = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BackgroundPath
        {
            get => _backgroundPath;
            set
            {
                _backgroundPath = value;
                if (File.Exists(value))
                {
                    using var stream = File.OpenRead(value);
                    using var image = Image.FromStream(stream);
                    _background = new Bitmap(image);
                }
                Invalidate();
            }
        }

        public LoginSurface()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (_background is not null)
            {
                e.Graphics.DrawImage(_background, CoverRect(_background.Size, ClientSize));
            }
            else
            {
                using var fallback = new LinearGradientBrush(ClientRectangle, Color.FromArgb(16, 28, 46), Color.FromArgb(48, 119, 156), 35F);
                e.Graphics.FillRectangle(fallback, ClientRectangle);
            }

            using var shade = new LinearGradientBrush(ClientRectangle, Color.FromArgb(215, 9, 18, 32), Color.FromArgb(130, 9, 18, 32), 0F);
            e.Graphics.FillRectangle(shade, ClientRectangle);
            using var rightGlow = new LinearGradientBrush(ClientRectangle, Color.FromArgb(30, 80, 210, 255), Color.FromArgb(0, 80, 210, 255), 180F);
            e.Graphics.FillRectangle(rightGlow, ClientRectangle);
            base.OnPaint(e);
        }

        private static Rectangle CoverRect(Size image, Size target)
        {
            if (image.Width == 0 || image.Height == 0 || target.Width == 0 || target.Height == 0)
            {
                return new Rectangle(Point.Empty, target);
            }

            var scale = Math.Max(target.Width / (float)image.Width, target.Height / (float)image.Height);
            var width = (int)(image.Width * scale);
            var height = (int)(image.Height * scale);
            return new Rectangle((target.Width - width) / 2, (target.Height - height) / 2, width, height);
        }
    }
}
