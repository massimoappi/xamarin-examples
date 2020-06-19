using System;

namespace ForegroundService
{
	public static class Constants
	{
		public const int DELAY_BETWEEN_LOG_MESSAGES = 1000; // milliseconds
		public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
		public const string SERVICE_STARTED_KEY = "has_service_been_started";
		public const string SERVICE_BOUNDED_KEY = "has_service_been_bounded";
		public const string BROADCAST_MESSAGE_KEY = "broadcast_message";
		public const string NOTIFICATION_BROADCAST_ACTION = "ForegroundService.Notification.Action";

		public const string ACTION_START_SERVICE = "ForegroundService.Action.START_SERVICE";
		public const string ACTION_STOP_SERVICE = "ForegroundService.Action.STOP_SERVICE";
		public const string ACTION_RESTART_TIMER = "ForegroundService.Action.RESTART_TIMER";
		public const string ACTION_MAIN_ACTIVITY = "ForegroundService.Action.MAIN_ACTIVITY";
	}
}
