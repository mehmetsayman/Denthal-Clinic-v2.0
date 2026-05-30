using DisKlinigiYonetimSistemi.Data;
using DisKlinigiYonetimSistemi.Forms;

namespace DisKlinigiYonetimSistemi;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var store = new ClinicDataStore();
        store.InitializeAsync().GetAwaiter().GetResult();
        Application.Run(new LoginForm(store));
    }    
}
