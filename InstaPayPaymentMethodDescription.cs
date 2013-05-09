using System;
using Gmc.Fido.Payment.Api;

namespace Payment.Dummy
{
	public class InstaPayPaymentMethodDescription : MarshalByRefObject, IPaymentMethodDescription
	{
		/// <summary>
		/// Display name
		/// </summary>
		public string GetUserFriendlyName()
		{
			return "InstaPay";
		}

		/// <summary>
		/// Defines settings that needs to be set per application. Typically URL to service, merchant code, ..
		/// </summary>
		public PaymentSettingsEntry[] GetSettingsDescription()
		{
			return Settings.GetDescription();
		}
	}
}