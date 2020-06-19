using Android.OS;

namespace ForegroundService
{
  public class ForegroundServiceBinder<T>: Binder
  { 
    // Classe di servizio che implementa la classe Binder di sistema e che serve per passare il riferimento del servizio 
    // (tramite evento OnBind() chiamato dal sistema) alla classe connector istanziata dalla Activity che richiede il Bind 
    // al servizio stesso
    public T Service { get; private set; }

    public ForegroundServiceBinder(T service)
    {
      // Memorizza riferimento al servizio che ha generato l'istanza di questo Binder
      Service = service;
    }
  }
}