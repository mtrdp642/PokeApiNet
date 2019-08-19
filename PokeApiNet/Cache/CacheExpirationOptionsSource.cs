using System;
using System.Collections.Generic;

namespace PokeApiNet.Cache
{
    internal sealed class CacheExpirationOptionsSource : IObservable<CacheExpirationOptions>
    {
        private readonly List<IObserver<CacheExpirationOptions>> observers;

        public CacheExpirationOptionsSource()
        {
            this.observers = new List<IObserver<CacheExpirationOptions>>();
        }

        public IDisposable Subscribe(IObserver<CacheExpirationOptions> observer)
        {
            if (!this.observers.Contains(observer))
            {
                this.observers.Add(observer);
            }
            return new Unsubscriber(this.observers, observer);
        }

        public void UpdateOptions(CacheExpirationOptions value)
        {
            foreach (var observer in observers)
            {
                observer.OnNext(value);
            }
        }

        public void CloseStream()
        {
            foreach (var observer in observers)
            {
                observer.OnCompleted();
            }
            observers.Clear();
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly IList<IObserver<CacheExpirationOptions>> _observers;
            private readonly IObserver<CacheExpirationOptions> _observer;

            public Unsubscriber(IList<IObserver<CacheExpirationOptions>> observers, IObserver<CacheExpirationOptions> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (this._observer != null && this._observers.Contains(_observer))
                {
                    this._observers.Remove(_observer);
                }
            }
        }
    }
}
