using Android.App;
using Android.App.Job;
using Android.Content.Res;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using AndroidX.Annotations;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.Navigation;
using NLog;
using System;
using System.IO;

namespace AndroidXNavigationDrawer
{
  [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
  public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener                                               
  {
    private Toolbar               toolbar;
    private int                   toolbarHeight;
    private DrawerLayout          drawerLayout; 
    private ActionBarDrawerToggle drawerToggle; 

    private Logger logger;

    protected override void OnCreate(Bundle savedInstanceState)
    {
      base.OnCreate(savedInstanceState);
      Xamarin.Essentials.Platform.Init(this, savedInstanceState);

      // Log
      logger = NLog.LogManager.GetCurrentClassLogger();

      var configuration = new NLog.Config.LoggingConfiguration();
      var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "${specialfolder:folder=Personal}/app.log" };
      var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

      configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
      configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
      
      LogManager.Configuration = configuration;

      logger = LogManager.GetCurrentClassLogger();
      logger.Trace("App started");
      logger.Debug("App started");
      logger.Info("App started");
      logger.Warn("App started");
      logger.Error("App started");
      logger.Fatal("App started");

      var logFile = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "app.log");
      if(File.Exists(logFile))
      {
        string sdcard = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Download");
        //File.Copy(logFile, sdcard, true);
      }
            
      // Set our view from the "main" layout resource
      SetContentView(Resource.Layout.activity_main);

      // Inizializza View
      InitializeView();

      // Inizializza Toolbar
      InitializeToolbar();

      // Inizializza NavigationView
      InitializeNavigationView();      

      //HomeFragment fragment = new HomeFragment();
      //FragmentTransaction fragmentTransaction = getSupportFragmentManager().beginTransaction();
      //fragmentTransaction.replace(R.id.frame_layout, fragment, "Home");
      //fragmentTransaction.commit();
    }

    private void InitializeView()
    { 

      // Recupera riferimenti agli oggetti della view
      toolbar = (Toolbar)FindViewById(Resource.Id.toolbar);
      drawerLayout = (DrawerLayout)FindViewById(Resource.Id.rootLayout);

      //this.Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

      //this.Window.SetFlags(WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.LayoutInScreen, WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.LayoutInScreen);

      toolbarHeight = toolbar.LayoutParameters.Height;
      /*
      this.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LayoutFullscreen |
                                                 (StatusBarVisibility)SystemUiFlags.LayoutStable |
                                                 (StatusBarVisibility)SystemUiFlags.LayoutHideNavigation;
      */

      //this.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LayoutFullscreen | (StatusBarVisibility)SystemUiFlags.LayoutStable;

      /*
      this.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LayoutFullscreen |
                                                 (StatusBarVisibility)SystemUiFlags.LightStatusBar | 
                                                 (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
      */
    }
    

    private void InitializeToolbar()
    { 
      // Imposta la toolbar come ActionBar dell'Activity      
      SetSupportActionBar(toolbar);
     
      // Imposta icona custom
      //SupportActionBar.SetDisplayShowHomeEnabled(true);
      //SupportActionBar.SetLogo(Resource.Drawable.logo);
      //SupportActionBar.SetDisplayUseLogoEnabled(true);
      
      // Recupera riferimento al DrawerLayout dell'Activity e crea un ActionBarDrawerToggle per la
      // gestione automatizzata dell'apertura/chiusura del NavigationView
      drawerLayout = (DrawerLayout)FindViewById(Resource.Id.rootLayout);
      drawerToggle = new ActionBarDrawerToggle(this, drawerLayout, toolbar, Resource.String.nav_open, Resource.String.nav_close);

      // Aggancia il toggle al DrawerLayout per applicare la gestione
      drawerLayout.AddDrawerListener(drawerToggle);
      // Sinronizza lo stato del togle a quello del DrawerLayout
      drawerToggle.SyncState();     

      //this.Window.DecorView.GetWindowVisibleDisplayFrame(out Android.Graphics.Rect rect);
    }



    private void InitializeNavigationView()
    { 
      // NavigationView
      NavigationView navigationView = (NavigationView)FindViewById(Resource.Id.nav_view);
      navigationView.SetNavigationItemSelectedListener(this);
    }

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
      // 
      if (drawerToggle.OnOptionsItemSelected(item))
        return true;
      return base.OnOptionsItemSelected(item);
    }

    public override bool OnCreateOptionsMenu(IMenu menu)
    {
      MenuInflater.Inflate(Resource.Menu.toolbar_menu, menu);
      return true;
    }

    public bool OnNavigationItemSelected(IMenuItem menuItem) 
    {
      // Selezione di un elemento nella NavigationView
      switch(menuItem.ItemId)
      { 
        case Resource.Id.nav_account:
            //HomeFragment fragment = new HomeFragment();
            //FragmentTransaction fragmentTransaction = getSupportFragmentManager().beginTransaction();
            //fragmentTransaction.replace(R.id.frame_layout, fragment, "Home");
            //fragmentTransaction.commit();

          break;
      }

      // Forza chiusura della NavigationView
      drawerLayout.CloseDrawer(GravityCompat.Start);
      return true;
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
      Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

      base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
  }
}