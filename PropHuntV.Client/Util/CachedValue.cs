using System;

namespace PropHuntV.Util
{
	public abstract class CachedValue<T>
	{
		private DateTime _lastUpdate = DateTime.MinValue;
		private readonly long _timeoutInterval;

		private T _cachedValue;

		public CachedValue( long timeoutMs ) {
			_timeoutInterval = timeoutMs;
		}

		public T Value
		{
			get {
				if( (DateTime.Now - _lastUpdate).TotalMilliseconds > _timeoutInterval ) {
					_cachedValue = Update();
					_lastUpdate = DateTime.Now;
				}
				return _cachedValue;
			}
		}

		protected abstract T Update();
	}
}
