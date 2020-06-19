using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Util;
using System;
using System.Runtime.CompilerServices;
using Android.Accounts;

namespace ForegroundService
{
	[Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
    #region Oggetti e variabili

    private static readonly string TAG = typeof(MainActivity).FullName;

		private Button		startServiceButton;
		private Button		stopServiceButton;
		private Button		bindServiceButton;
		private Button		unbindServiceButton;
		private TextView	deviceTimeLabel;
		
		private Intent	serviceIntent;
		private ForegroundService service;
		private ForegroundServiceConnector<ForegroundService> serviceConnector;
		
		private bool		isStarted = false;
		private bool		isBounded = false;

		#endregion

    #region Lifecycle dell'Activity

		protected override void OnCreate(Bundle savedInstanceState)
		{
			// Costruttore dell'Activity
			base.OnCreate(savedInstanceState);

			Log.Info(TAG, "OnCreate() fired");

			// Carica il Layout
			SetContentView(Resource.Layout.MainActivity);
			
			// Gestione dell'eventuale Intent passato all'Activity
			OnNewIntent(this.Intent);

			// Recupera stato del servizio da istanza precedente
			if (savedInstanceState != null)
			{
				isStarted = savedInstanceState.GetBoolean(Constants.SERVICE_STARTED_KEY, false);
				isBounded = savedInstanceState.GetBoolean(Constants.SERVICE_BOUNDED_KEY, false);
			}

			// Inizializza oggetti
			InitializeObjects();

			// Inizializza controlli
			InitializeControls();

			// Imposta stato dei controlli
			SetControlsState();
		}

		protected override void OnNewIntent(Intent intent)
		{
			if (intent == null)
			{
				return;
			}

			var bundle = intent.Extras;
			if (bundle != null)
			{
				if (bundle.ContainsKey(Constants.SERVICE_STARTED_KEY) )
				{
					isStarted = true;
				}
			}

			//startServiceButton.Enabled = !isStarted;
			//stopServiceButton.Enabled = isStarted;
		}

    public override void OnBackPressed()
		{
			// Evento alzato quando l'utente preme il tasto 'Back' nella StatusBar di Android
			// Per evitare che questo scateni l'OnDestroy() dell'Activity e che la ripresa dalla Notification comporti la
			// creazione di una nuova istanza di questa Activity, inibisco il 'Back' evitando di passare l'evento alla
			// classe base
			// base.OnBackPressed();
		}

    protected override void OnRestart()
    {
			// Evento del LifeCycle, solo per trace
			Log.Info(TAG, "OnRestart() fired");
      base.OnRestart();
    }

    protected override void OnStart()
    {
			// Evento del LifeCycle, solo per trace
			Log.Info(TAG, "OnStart() fired");
      base.OnStart();
    }

    protected override void OnStop()
    {
			// Evento del LifeCycle, solo per trace
			Log.Info(TAG, "OnStop() fired");
      base.OnStop();
    }

    protected override void OnPause()
    {
			// Evento del LifeCycle, solo per trace
			Log.Info(TAG, "OnPause() fired");
      base.OnPause();
    }

    protected override void OnDestroy()
		{
			// Evento del LifeCycle, solo per trace
			Log.Info(TAG, "OnDestroy() fired");

			// Qui l'Activity viene scaricata, ma il servizio (se in esecuzione) continua a girare
			//StopService(serviceIntent);

			// Scarico oggetti dell'Activity
			DisposeObjects();

			base.OnDestroy();
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			// Memorizzo stato attuale del servizio per eventuale ripresa futura
			outState.PutBoolean(Constants.SERVICE_STARTED_KEY, isStarted);
			outState.PutBoolean(Constants.SERVICE_BOUNDED_KEY, isBounded);
			base.OnSaveInstanceState(outState);
		}

    #endregion

    #region Eventi dei controlli

    void StartServiceButton_Click(object sender, System.EventArgs e)
		{
			StartSvc();			
			SetControlsState();
		}

		void StopServiceButton_Click(object sender, System.EventArgs e)
		{
			StopSvc();
			SetControlsState();
		}

		void BindServiceButton_Click(object sender, System.EventArgs e)
		{
			// Forza bind al servizio
			BindSvc(Bind.None);
			SetControlsState();
		}

		void UnbindServiceButton_Click(object sender, System.EventArgs e)
		{
			// Forza unbind al servizio
			UnbindSvc();
			SetControlsState();
		}

    #endregion

    #region Gestione del servizio

    private void StartSvc()
    {		
			ComponentName component;

      // Avvia servizio, prestando attenzione alla versione di Android in esecuzione

      if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
      {
				// Versione Android Oreo o successiva; il servizio DEVE essere avviato come servizio di Foreground
				Log.Info(TAG, "StartSvc(): starting service via StartForegroundService()..");
        component = StartForegroundService(serviceIntent);				
      }
      else
      {
				// Versione precedente ad Android Oreo, NON è necessario far partire il servizio come Foreground
				Log.Info(TAG, "StartSvc(): starting service via StartService()..");
        component = StartService(serviceIntent);
      }

      Log.Info(TAG, $"StartSvc(): service started, className={component.ClassName}");

			isStarted = true;
    }

    private void StopSvc()
    {
			// Termina servizio
			Log.Info(TAG, $"StopSvc(): stopping service..");      

			// N.B: se decido di terminare un servizio, devo effettuare preventivamente anche l'Unbind, perchè fino a quando almeno
			// una Activity (questa o altra) rimane bindata all'istanza del servizio stesso, anche se viene chiamato il metodo 
			// StopService() di fatto il servizio continua a vivere e l'evento Service.OnDestroy() non viene mai chiamato
			
			// Effettua Unbind preventivo dal servizio di questa Activity
			UnbindSvc();
      
			// Forza stop del servizio
			bool result = StopService(serviceIntent);
      
			//Log.Info(TAG, $"StopSvc(): result={result}");
			isStarted = false;
    }

    private void BindSvc(Bind bindFlags)
    {
			// Effettuo il bind di questa Activity al servizio
			Log.Info(TAG, $"BindSvc(): bindFlags={bindFlags.ToString()}");

      // Utilizzo del bind:
      // Bind.None: se il servizio NON è già istanziato, esso NON viene forzatamente istanziato, ma il sistema registra comunque 
			//            la richiesta di bind da parte dell'Activity. Se e quando il servizio verrà istanziato (es: tramite StartService)
			//            scatterà automaticamente il Bind già registrato e l'Activity verrà notificata per mezzo del foregroundServiceConnector.
			//            Se il servizio invece è già in esecuzione il Bind avverrà immediatamente e seguirà il normale flusso logico
      // Bind.AutoCreate: se il servizio NON è già istanziato ne verrà generata una nuova istanza ed il Bind seguirà il normale flusso
      //            logico
      bool result = BindService(new Intent(this, typeof(ForegroundService)), serviceConnector, bindFlags);

			Log.Info(TAG, $"BindSvc(): result={result}");

      // Sarà l'evento OnServiceConnected() del connettore chiamato dal sistema ad informare questa Activity che il Bind al servizio
			// è andato a buon fine; lo stato del Bind sarà gestito in tale evento. Per l'Unbind invece il corrispondente evento 
			// OnServiceDisconnected() non viene stranamente chiamato dal sistema!
    }

    private void UnbindSvc()
    {
			// Esce se servizio non bindato
			if (! isBounded) return;

			// Effettuo l'unbind di questa Activity dal servizio
			Log.Info(TAG, $"UnbindSvc()");

      // Unbind del servizio
      UnbindService(serviceConnector);

			// Dato che l'UnbindService() chiamato da una Activity nei confronti del servizio sottoscritto non alza MAI l'evento
			// OnServiceDisconnected() del connector (sembra venga alzato SOLO se l'OS decide di killare l'Activity o il Service), 
			// allora simulo l'evento stesso per rimanere nel flusso logico standard..
			ServiceConnector_Disconnected();			      
    }

		private void ServiceConnector_Connected(ForegroundService service)
		{ 
			// Servizio correttamente bindato dall'Activity, in 'service' ho un riferimento diretto all'istanza del servizio
			Log.Info(TAG, $"ServiceConnector_Connected()");

			// Memorizza riferimenti al servizio e sottoscrive gli eventi
			this.service = service;
      this.service.WorkCompleted += Service_WorkCompleted;

			// Imposta variabili semaforo
			isBounded = true;
			SetControlsState();
		}    

    private void ServiceConnector_Disconnected()
		{ 
			// Servizio correttamente unbindato dall'Activity
			Log.Info(TAG, $"ServiceConnector_Disconnected()");

			// Scarica i riferimenti al servizio disconnesso
			this.service.WorkCompleted -= Service_WorkCompleted;
			this.service = null;

			// Imposta variabili semaforo						
			isBounded = false;
			SetControlsState();
		}

		private void Service_WorkCompleted(string stuff)
    {
			// Metodo dummy del servizio
      deviceTimeLabel.Text = stuff;
    }

    #endregion

    #region Metodi dell'Activity

		private void InitializeObjects()
		{ 
			// Istanzia Intent per referenziare il servizio
			serviceIntent = new Intent(this, typeof(ForegroundService));
			serviceIntent.SetAction(Constants.ACTION_START_SERVICE);

      // Prepara una unica istanza della classe di connessione al servizio
      serviceConnector	= new ForegroundServiceConnector<ForegroundService>();
      serviceConnector.Connected    += ServiceConnector_Connected;
      serviceConnector.Disconnected += ServiceConnector_Disconnected;
		}

		private void DisposeObjects()
		{ 
			// Scarica oggetti
			serviceIntent.Dispose();
			serviceIntent = null;

      serviceConnector.Connected    -= ServiceConnector_Connected;
      serviceConnector.Disconnected -= ServiceConnector_Disconnected;
			serviceConnector = null;
		}

		private void InitializeControls()
		{
			// Recupera riferimenti ai controlli della Activity
			startServiceButton				= FindViewById<Button>(Resource.Id.start_timestamp_service_button);
			stopServiceButton					= FindViewById<Button>(Resource.Id.stop_timestamp_service_button);
			bindServiceButton					= FindViewById<Button>(Resource.Id.bind_timestamp_service_button);
			unbindServiceButton				= FindViewById<Button>(Resource.Id.unbind_timestamp_service_button);
			deviceTimeLabel	  				= FindViewById<TextView>(Resource.Id.device_time_label);

			// Sottoscrive eventi dei controlli			
			startServiceButton.Click	+= StartServiceButton_Click;
			stopServiceButton.Click		+= StopServiceButton_Click;
			bindServiceButton.Click		+= BindServiceButton_Click;
			unbindServiceButton.Click += UnbindServiceButton_Click;
		}

    private void SetControlsState()
		{ 
			startServiceButton.Enabled	= (! isStarted);
			stopServiceButton.Enabled		= isStarted;
			
			bindServiceButton.Enabled		= (! isBounded);
			unbindServiceButton.Enabled	= isBounded;
		}

		#endregion
	}	
}

