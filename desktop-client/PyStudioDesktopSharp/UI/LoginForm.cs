using PyStudioDesktopSharp.Services;

namespace PyStudioDesktopSharp.UI;

public sealed class LoginForm : Form
{
    private readonly ApiClient _api = new();
    private bool _showingRegister = false;

    // Login controls
    private Panel _loginPanel = null!;
    private TextBox _emailBox = null!;
    private TextBox _passwordBox = null!;

    // Register controls
    private Panel _registerPanel = null!;
    private TextBox _firstNameBox = null!;
    private TextBox _lastNameBox = null!;
    private TextBox _regEmailBox = null!;
    private TextBox _regPasswordBox = null!;
    private TextBox _regConfirmBox = null!;
    private Label _regErrorLabel = null!;
    private Label _loginErrorLabel = null!;

    public ApiClient AuthenticatedApi => _api;

    public LoginForm()
    {
        Text = "PyStudio Desktop - Iniciar sesión";
        Width = 480;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(18, 18, 18);

        BuildLoginPanel();
        BuildRegisterPanel();

        Controls.Add(_loginPanel);
        Controls.Add(_registerPanel);

        ShowLogin();
    }

    private void BuildLoginPanel()
    {
        _loginPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40) };
        _loginPanel.BackColor = Color.FromArgb(18, 18, 18);

        var title = new Label
        {
            Text = "PyStudio Desktop",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Left = 100,
            Top = 40
        };

        var subtitle = new Label
        {
            Text = "Iniciá sesión para continuar",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            AutoSize = true,
            Left = 120,
            Top = 80
        };

        var emailLabel = MakeLabel("Correo", 130);
        _emailBox = MakeTextBox(160, "estudiante@tec.ac.cr");

        var passLabel = MakeLabel("Contraseña", 210);
        _passwordBox = MakeTextBox(240, "");
        _passwordBox.PasswordChar = '*';

        _loginErrorLabel = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(239, 68, 68),
            AutoSize = true,
            Left = 40,
            Top = 285,
            Width = 380
        };

        var loginBtn = new Button
        {
            Text = "Iniciar sesión",
            Left = 40, Top = 310,
            Width = 380, Height = 44,
            BackColor = Color.White,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        loginBtn.FlatAppearance.BorderSize = 0;
        loginBtn.Click += async (_, _) => await LoginAsync();

        var registerLink = new LinkLabel
        {
            Text = "¿No tenés cuenta? Registrate aquí",
            Left = 120, Top = 365,
            AutoSize = true,
            LinkColor = Color.FromArgb(96, 165, 250),
            Font = new Font("Segoe UI", 9)
        };
        registerLink.LinkClicked += (_, _) => ShowRegister();

        _loginPanel.Controls.AddRange(new Control[]
        {
            title, subtitle, emailLabel, _emailBox,
            passLabel, _passwordBox, _loginErrorLabel,
            loginBtn, registerLink
        });
    }

    private void BuildRegisterPanel()
    {
        _registerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40) };
        _registerPanel.BackColor = Color.FromArgb(18, 18, 18);

        var title = new Label
        {
            Text = "Crear Cuenta",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Left = 140,
            Top = 20
        };

        var subtitle = new Label
        {
            Text = "Ingresá tus datos para crear tu cuenta",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            AutoSize = true,
            Left = 110,
            Top = 58
        };

        var firstNameLabel = MakeLabel("Nombre", 90);
        _firstNameBox = new TextBox
        {
            Left = 40, Top = 115,
            Width = 175, Height = 36,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lastNameLabel = new Label
        {
            Text = "Apellido",
            ForeColor = Color.White,
            AutoSize = true,
            Left = 225, Top = 90,
            Font = new Font("Segoe UI", 9)
        };
        _lastNameBox = new TextBox
        {
            Left = 225, Top = 115,
            Width = 195, Height = 36,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var emailLabel = MakeLabel("Correo", 155);
        _regEmailBox = MakeTextBox(182, "");

        var passLabel = MakeLabel("Contraseña", 222);
        _regPasswordBox = MakeTextBox(249, "");
        _regPasswordBox.PasswordChar = '*';

        var confirmLabel = MakeLabel("Confirmar contraseña", 289);
        _regConfirmBox = MakeTextBox(316, "");
        _regConfirmBox.PasswordChar = '*';

        _regErrorLabel = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(239, 68, 68),
            AutoSize = false,
            Left = 40, Top = 352,
            Width = 380, Height = 20
        };

        var hint = new Label
        {
            Text = "Mínimo 8 caracteres, una letra minúscula, una mayúscula y un número.",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7.5f),
            Left = 40, Top = 372,
            Width = 380, AutoSize = false
        };

        var registerBtn = new Button
        {
            Text = "Registrarme",
            Left = 40, Top = 395,
            Width = 380, Height = 44,
            BackColor = Color.White,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        registerBtn.FlatAppearance.BorderSize = 0;
        registerBtn.Click += async (_, _) => await RegisterAsync();

        var loginLink = new LinkLabel
        {
            Text = "Si ya tenés una cuenta, ingresá aquí",
            Left = 110, Top = 445,
            AutoSize = true,
            LinkColor = Color.FromArgb(96, 165, 250),
            Font = new Font("Segoe UI", 9)
        };
        loginLink.LinkClicked += (_, _) => ShowLogin();

        _registerPanel.Controls.AddRange(new Control[]
        {
            title, subtitle,
            firstNameLabel, _firstNameBox,
            lastNameLabel, _lastNameBox,
            emailLabel, _regEmailBox,
            passLabel, _regPasswordBox,
            confirmLabel, _regConfirmBox,
            hint, _regErrorLabel, registerBtn, loginLink
        });
    }

    private void ShowLogin()
    {
        _showingRegister = false;
        _loginPanel.Visible = true;
        _registerPanel.Visible = false;
        _loginPanel.BringToFront();
        Height = 430;
    }

    private void ShowRegister()
    {
        _showingRegister = true;
        _registerPanel.Visible = true;
        _loginPanel.Visible = false;
        _registerPanel.BringToFront();
        Height = 530;
    }

    private async Task LoginAsync()
    {
        _loginErrorLabel.Text = "";
        string email = _emailBox.Text.Trim();
        string password = _passwordBox.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _loginErrorLabel.Text = "Correo y contraseña son obligatorios.";
            return;
        }

        try
        {
            _api.SetBaseUrl("http://192.9.149.63/backend/api");
            var data = await _api.LoginAsync(email, password);

            if (data.User?.Role != "student")
            {
                _loginErrorLabel.Text = "Acceso restringido a estudiantes.";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _loginErrorLabel.Text = ex.Message;
        }
    }

    private async Task RegisterAsync()
    {
        _regErrorLabel.Text = "";
        string firstName = _firstNameBox.Text.Trim();
        string lastName = _lastNameBox.Text.Trim();
        string email = _regEmailBox.Text.Trim();
        string password = _regPasswordBox.Text;
        string confirm = _regConfirmBox.Text;

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _regErrorLabel.Text = "Todos los campos son obligatorios.";
            return;
        }

        if (password != confirm)
        {
            _regErrorLabel.Text = "Las contraseñas no coinciden.";
            return;
        }

        if (password.Length < 8)
        {
            _regErrorLabel.Text = "La contraseña debe tener al menos 8 caracteres.";
            return;
        }

        try
        {
            _api.SetBaseUrl("http://192.9.149.63/backend/api");
            string fullName = $"{firstName} {lastName}";
            await _api.RegisterStudentAsync(fullName, email, password);
            ShowLogin();
            _loginErrorLabel.Text = "";
            _emailBox.Text = email;
        }
        catch (Exception ex)
        {
            _regErrorLabel.Text = ex.Message;
        }
    }

    private static Label MakeLabel(string text, int top) => new()
    {
        Text = text,
        ForeColor = Color.White,
        AutoSize = true,
        Left = 40,
        Top = top,
        Font = new Font("Segoe UI", 9)
    };

    private static TextBox MakeTextBox(int top, string placeholder) => new()
    {
        Left = 40,
        Top = top,
        Width = 380,
        Height = 36,
        Font = new Font("Segoe UI", 10),
        BackColor = Color.FromArgb(45, 45, 45),
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Text = placeholder
    };
}