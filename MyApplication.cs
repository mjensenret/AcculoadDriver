namespace AcculoadOASDriver.My
{
	internal class MyApplication : Microsoft.VisualBasic.ApplicationServices.ApplicationBase
	{
		[global::System.Diagnostics.DebuggerStepThroughAttribute()]
		public MyApplication() : base()
		{
		}

		private static MyApplication MyApp;
		internal static MyApplication Application
		{
			get
			{
				if (MyApp == null)
					MyApp = new MyApplication();

				return MyApp;
			}
		}
	}
}