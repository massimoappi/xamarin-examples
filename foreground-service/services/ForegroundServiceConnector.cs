using System;

using Android.Content;
using Android.OS;
using Android.Util;

namespace ForegroundService
{
	public class ForegroundServiceConnector<T> : Java.Lang.Object, IServiceConnection
	{
    // Classe di servizio che implementa l'interfaccia IServiceConnection di sistema e che serve per notificare all'Activity
		// che ha generato questa istanza lo stato del servizio per il quale è stato effettuato il bind

		#region Oggetti e variabili

		static readonly string TAG = typeof(ForegroundServiceConnector<T>).FullName;

		public bool IsConnected { get; private set; }
		public ForegroundServiceBinder<T> Binder { get; private set; }

		public delegate void ConnectedEventHandler(T service);
		public delegate void DisconnectedEventHandler();

		public event ConnectedEventHandler		Connected;
		public event DisconnectedEventHandler Disconnected;

    #endregion

    #region Costruttore

    public ForegroundServiceConnector()
		{
			Log.Info(TAG, $"OnServiceConnected(): ctor");

			IsConnected = false;
			Binder			= null;
		}

    #endregion

    #region Implementazione interfaccia IServiceConnection

    public void OnServiceConnected(ComponentName name, IBinder service)
		{	
			// Metodo chiamato dal sistema

			// Recupero riferimento all'interfaccia IBinder dell'oggetto ForegroundServiceBinder ritornata dalla chiamata
			// all'evento OnBind() nel service, che contiene il riferimento all'istanza del service stesso
			Binder = service as ForegroundServiceBinder<T>;
			IsConnected = (Binder != null);

			// Questo log sembra non comparire in console (!?!)
			Log.Info(TAG, $"OnServiceConnected(): className={name.ClassName}, isConnected={IsConnected}");

			// Alza evento e passa riferimento diretto al service
      Connected?.Invoke((T)Binder.Service);
		}

		public void OnServiceDisconnected(ComponentName name)
		{
			// Metodo chiamato dal sistema

			// Prestare attenzione che questo metodo non viene chiamato MAI nel normale flusso logico dell'Activity!!
			//
			// Called when a connection to the Service has been lost. This typically happens when the process hosting the service has crashed 
			// or been killed. This does not remove the ServiceConnection itself -- this binding to the service will remain active, and you will
			// receive a call to onServiceConnected(ComponentName, IBinder) when the Service is next running.

			IsConnected = false;
			Binder			= null;

			Log.Info(TAG, $"OnServiceDisconnected(): className={name.ClassName}, isConnected={IsConnected}");
			
			// Gira evento
      Disconnected?.Invoke();
		}

		#endregion
	}
}