using System;
using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;

namespace ForegroundService
{
	[Service(Enabled = true, IsolatedProcess = false)]
	public class ForegroundService : Service
	{
    // Classe che implementa il servizio vero e proprio

    #region Oggetti e variabili

    private static readonly string TAG = typeof(ForegroundService).FullName;

		private bool isStarted;
		private Handler backgroundWorker;
		private Action	backgroundWorkerCallback;

		public delegate void WorkCompletedEventHandler(string stuff);
		public event WorkCompletedEventHandler WorkCompleted;

    #endregion

    #region Proprietà

    public IBinder Binder { get; private set; }

    #endregion

    #region LyfeCycle del servizio

    public override void OnCreate()
		{
			// Questo evento viene alzato quando il servizio viene avviato esplicitamente con i metodi StartForegroundService(), 
			// StartService() oppure quando viene bindato con BindService(Bind.AutoCreate) e non era già stato precedentemente 
			// istanziato

			base.OnCreate();
			Log.Info(TAG, "OnCreate()");			

			// Simula un BackgorundWorker..
			backgroundWorker = new Handler();
			backgroundWorkerCallback = new Action(() => BackgroundWorkerCallback());
		}

		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
		{
			// Questo evento viene alzato quando il servizio viene avviato esplicitamente con i metodi StartForegroundService() 
			// oppure StartService()
			
			// Indica al sistema che deve gestire questo servizio come Sticky, cioè deve farlo ripartire il caso di kill
			StartCommandResult result = StartCommandResult.Sticky;

			Log.Info(TAG, $"OnStartCommand(): intent='{intent.Action}', startId={startId}");

			if (intent.Action.Equals(Constants.ACTION_START_SERVICE))
			{
				if (isStarted)
				{
					Log.Info(TAG, "OnStartCommand(): the service is already running!");
				}
				else 
				{					
					// Registra una nuova notifica di sistema
					RegisterNotification();
					
					// Avvia il BackgroundWorker
					backgroundWorker.PostDelayed(backgroundWorkerCallback, 1);
					isStarted = true;

					Log.Info(TAG, "OnStartCommand(): service started");
				}
			}
			else if (intent.Action.Equals(Constants.ACTION_STOP_SERVICE))
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					StopForeground(StopForegroundFlags.Remove);
				}
				StopSelf();
				isStarted = false;
				Log.Info(TAG, "OnStartCommand(): service stopped");
			}
			else if (intent.Action.Equals(Constants.ACTION_RESTART_TIMER))
			{
				Log.Info(TAG, "OnStartCommand(): timer restarted");				
			}				

			Log.Info(TAG, $"OnStartCommand(): returning result='{result}'");

			return result;
		}

		public override IBinder OnBind(Intent intent)
		{
			// Una nuova Activity ha richiesto il Bind a questa istanza del servizio

			Log.Info(TAG, $"OnBind(): intent='{intent.Action}'");

			// Genero e ritorno nuova istanza del ForegroundServiceBinder passando il riferimento a questo servizio
			Binder = new ForegroundServiceBinder<ForegroundService>(this);
			return Binder;
		}

    public override void OnRebind(Intent intent)
    {
			// Una Activity, che ha già precedentemente effettuato un Bind/Unbind, ha effettuato nuovamente il Bind
			Log.Info(TAG, $"OnRebind(): intent='{intent.Action}'");
      base.OnRebind(intent);
    }

    public override bool OnUnbind(Intent intent)
		{
			// Questo evento scatta SOLO quando l'ultima Activity che ha effettuato il Bind al servizio tramite BindService() 
			// effettua la chiamata al metodo UnbindService()
			//
			// Called when all clients have disconnected from a particular interface published by the service. The default implementation 
			// does nothing and returns false.
			base.OnUnbind(intent);

			// Ritornando true richiedo al sistema di chiamare nuovamente la OnBind() al successivo utilizzo della BindService()
			// da parte di una Activity che ha già effettuato precedentemente un Bind/Unbind
			bool callOnRebindOnNewConnections = true;			
			
			Log.Info(TAG, $"OnUnbind(): intent='{intent.Action}', callOnRebindOnNewConnections={callOnRebindOnNewConnections}");
			return callOnRebindOnNewConnections;
		}

		public override void OnDestroy()
		{
			Log.Info(TAG, "OnDestroy()");

			// Tutte le Activiti hanno effettuato l'Unbind dal servizio o il servizio viene fermato tramite la StopService()

			// Termino il BackgroundWorker
			backgroundWorker.RemoveCallbacks(backgroundWorkerCallback);

			// Rimuove notifica
			RemoveNotification();

			// Scarico oggetti
			Binder			= null;
			isStarted		= false;

			// Termina (internamente) il servizio
			StopSelf();

			base.OnDestroy();
		}

    #endregion

    #region Workload del servizio

    private void BackgroundWorkerCallback()
		{ 
			string deviceTime = DateTime.Now.ToString("hh:mm:ss.ffff");

			Log.Info(TAG, $"DoWork(): current device time: {deviceTime}");
			//Intent i = new Intent(Constants.NOTIFICATION_BROADCAST_ACTION);
			//i.PutExtra(Constants.BROADCAST_MESSAGE_KEY, msg);
			//Android.Support.V4.Content.LocalBroadcastManager.GetInstance(this).SendBroadcast(i);

			// Notifica evento
			WorkCompleted?.Invoke(deviceTime);
			
			// Rimette in schedulazione 
			if (isStarted) backgroundWorker.PostDelayed(backgroundWorkerCallback, Constants.DELAY_BETWEEN_LOG_MESSAGES);
		}

    #endregion

    #region Gestione del NotificationManager

    private void RegisterNotification()
		{
			// Registra una nuova notifica di sistema affinchè l'utente sia informato che il servizio è in esecuzione anche
			// se l'Activity dovesse essera mandata in background. Tramite la notifica sarà anche possibile riaccedere rapidamente
			// all'Activity senza rieseguirla esplicitamente
			//
			// N.B.: da Android Oreo la notifica di sistema è l'UNICO modo per assicurarsi che un servizio sia visto dal sistema
			// come servizio di Foreground ed evitare quindi che tenti di killarlo  

			Log.Info(TAG, "RegisterForegroundService(): building notification..");

			// Recupera riferimento al NotificationManager di sistema
			using (NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService))
			{
				Notification.Builder notificationBuilder;

				// In base alla versione corrente di Android..
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					// Genera un nuovo canale
					NotificationChannel channel = new NotificationChannel("channel_id", "channel_name", NotificationImportance.Default)
					{
						Description = "channel_desc"
					};

					// Crea ed associa il canale al NotificationManager
					Log.Info(TAG, "RegisterForegroundService(): registering Channel..");
					notificationManager.CreateNotificationChannel(channel);

					// Genera il NotificationBuilder passando il riferimento al canale creato
					notificationBuilder = new Notification.Builder(this, "channel_id");
				}
				else
				{
					// Genera il NotificationBuilder alla maniera classica
					notificationBuilder = new Notification.Builder(this);
				}

				// Genera notifica con tutte le relative proprietà
				Notification notification = notificationBuilder
					.SetContentTitle(Resources.GetString(Resource.String.app_name))
					.SetContentText(Resources.GetString(Resource.String.notification_text))
					.SetSmallIcon(Resource.Drawable.ic_stat_name)
					.SetContentIntent(BuildIntentToShowMainActivity())
					.SetOngoing(true)
					.AddAction(BuildRestartTimerAction())
					.AddAction(BuildStopServiceAction())
					.Build();

				// Se versione Android Oreo o successiva..
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					// Eleva questo servizio a 'servizio di foreground' per evitare che il sistema possa decidere di killarlo
					StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, notification);
				}
				else
				{
					// Servizio standard (Android precedenti ad Oreo non dovrebbero mai killarlo..)
					notificationManager.Notify(Constants.SERVICE_RUNNING_NOTIFICATION_ID, notification);
				}
			}

			Log.Info(TAG, "RegisterForegroundService(): notification builted");
		}

		private void RemoveNotification()
		{
			// Se versione Android Oreo o successiva..
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			{
				// Rimuove il servizio dalla stato di Foregorund e torna allo stato standard (Background, killabile)
				StopForeground(StopForegroundFlags.Remove);
			}

			// Rimuove la notifica
			using (NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService))
			{ 
				notificationManager.Cancel(Constants.SERVICE_RUNNING_NOTIFICATION_ID);			
			}
		}

    #endregion

    #region Gestione dell'interazione utente con la notifica

    /// <summary>
    /// Builds a PendingIntent that will display the main activity of the app. This is used when the 
    /// user taps on the notification; it will take them to the main activity of the app.
    /// </summary>
    /// <returns>The content intent.</returns>
    PendingIntent BuildIntentToShowMainActivity()
		{
			var notificationIntent = new Intent(this, typeof(MainActivity));
			notificationIntent.SetAction(Constants.ACTION_MAIN_ACTIVITY);
			notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
			notificationIntent.PutExtra(Constants.SERVICE_STARTED_KEY, true);

			var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.UpdateCurrent);
			return pendingIntent;
		}

		/// <summary>
		/// Builds a Notification.Action that will instruct the service to restart the timer.
		/// </summary>
		/// <returns>The restart timer action.</returns>
		Notification.Action BuildRestartTimerAction()
		{
			PendingIntent restartTimerPendingIntent;
			var restartTimerIntent = new Intent(this, GetType());
			restartTimerIntent.SetAction(Constants.ACTION_RESTART_TIMER);

			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				restartTimerPendingIntent = PendingIntent.GetForegroundService(this, 0, restartTimerIntent, 0);
			else
				restartTimerPendingIntent = PendingIntent.GetService(this, 0, restartTimerIntent, 0);

			var builder = new Notification.Action.Builder(Resource.Drawable.ic_action_restart_timer,
											  GetText(Resource.String.restart_timer),
											  restartTimerPendingIntent);

			return builder.Build();
		}

		/// <summary>
		/// Builds the Notification.Action that will allow the user to stop the service via the
		/// notification in the status bar
		/// </summary>
		/// <returns>The stop service action.</returns>
		Notification.Action BuildStopServiceAction()
		{
			PendingIntent stopServicePendingIntent;
			Intent stopServiceIntent = new Intent(this, typeof(ForegroundService));
			stopServiceIntent.SetAction(Constants.ACTION_STOP_SERVICE);

			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				stopServicePendingIntent = PendingIntent.GetForegroundService(this, 0, stopServiceIntent, 0);
			else
				stopServicePendingIntent = PendingIntent.GetService(this, 0, stopServiceIntent, 0);

			var builder = new Notification.Action.Builder(Android.Resource.Drawable.IcMediaPause,
														  GetText(Resource.String.stop_service),
														  stopServicePendingIntent);
			return builder.Build();

		}

		#endregion
	}
}
