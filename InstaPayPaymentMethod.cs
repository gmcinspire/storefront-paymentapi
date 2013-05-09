using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Gmc.Fido.Payment.Api;

namespace Payment.Dummy
{
	public class InstaPayPaymentMethod : MarshalByRefObject, IPaymentMethod
	{
		#region Settings
		Settings _settings;

		/// <summary>
		/// Settings collected in StoreFront administration. Define required fields in <see cref="IPaymentMethodDescription.GetSettingsDescription"/>
		/// </summary>
		public void SetSettings(Dictionary<string, string> settings)
		{
			_settings = new Settings(settings);
		}

		/// <summary>
		/// Validates payment method configuration. PaymentMethod cannot be activated before settings are valid.
		/// </summary>
		/// <param name="portalLocale">Current user locale.</param>
		/// <param name="settings">Settings to validate.</param>
		/// <returns>List of error messages.</returns>
		public string[] ValidateSettings(string portalLocale, Dictionary<string, string> settings)
		{
			var s = new Settings(settings);
			return s.Validate();
		}
		#endregion

		PaymentStatus PaymentStatus
		{
			get
			{
				return _settings.Pay ? PaymentStatus.Success : PaymentStatus.Cancelled;
			}
		}

		/// <summary>
		/// Restrict currencies that this payment method with current settings (e.g. specific merchant account) can work with.
		/// </summary>
		/// <param name="code">Checked currency code</param>
		public bool IsCurrencySupported(string code)
		{
			return _settings.SupportedCurrencies.Contains(code);
		}

		/// <summary>
		/// Creates optional setup form.
		/// </summary>
		/// <param name="order">Order info (id, price to be paid, ..)</param>
		/// <param name="portalLocale">Current user locale.</param>
		/// <returns>Content of setup form as HTML string or <code>null</code> when no setup is needed.</returns>
		public string CreateSetupForm(Order order, string portalLocale)
		{
			if (_settings.UseSetup)
				return "<input type='text' name='SomeParameter' />";
			return null;
		}

		/// <summary>
		/// Validates user input from form created in <see cref="CreateSetupForm"/>.
		/// </summary>
		/// <param name="order">Order info (id, price to be paid, ..)</param>
		/// <param name="portalLocale">Current user locale.</param>
		/// <param name="setupForm">Form values to be validated.</param>
		/// <returns>Array of error messages to be displayed on the setup form.</returns>
		public string[] ValidateSetupForm(Order order, string portalLocale, NameValueCollection setupForm)
		{
			if (!_settings.UseSetup)
				throw new InvalidOperationException("Should be called only to test setup form output, none was generated.");
			if (string.IsNullOrEmpty(setupForm["SomeParameter"]))
				return new[] { "There should be something filled." };
			return new string[0];
		}

		#region ProcessPayment
		/// <summary>
		/// Prepares payment form that will be sent (by user) to payment portal. Can establish a payment transaction if payment process requires it.
		/// </summary>
		/// <param name="order">Order info (id, price to be paid, ..)</param>
		/// <param name="portalLocale">Current user locale, can be send to payment portal to enforce the same.</param>
		/// <param name="urls">Urls that should be provided to payment portal to notify Storefront about success or to return back to portal. Notification is further processed by the <see cref="GetPaymentInfo"/>.</param>
		/// <param name="setupForm">Custom setup of payment method, can be <code>null</code> when no setup requested. See <see cref="CreateSetupForm"/></param>
		/// <returns>Payment form (to be send) plus optionally id of established transaction (to be stored).</returns>
		public PreparedPayment PreparePayment(Order order, string portalLocale, ReturnUrls urls, NameValueCollection setupForm)
		{
			var paymentForm = PaymentForm.Post(
				_settings.Pay ? urls.Notify : urls.Cancel,
				CreatePaymentFormFields(order, setupForm));
			return new PreparedPayment(paymentForm, EncodePriceAndStatusIntoTransaction(order.TotalPriceIncludingTax, PaymentStatus));
		}

		PaymentFormField[] CreatePaymentFormFields(Order order, NameValueCollection setupForm)
		{
			var paymentFormFields = new List<PaymentFormField>
				{
					new PaymentFormField("orderId", order.Id.ToString(CultureInfo.InvariantCulture)), 
					new PaymentFormField("price", order.TotalPriceIncludingTax.ToString(CultureInfo.InvariantCulture)),
					new PaymentFormField("currencyCode", order.CurrencyCode)
				};
			if (_settings.UseSetup)
				paymentFormFields.Add(new PaymentFormField("SomeParameter", setupForm["SomeParameter"]));
			return paymentFormFields.ToArray();
		}
		#endregion

		/// <summary>
		/// Standalone check of the payment status requested by user. Checks the real status with payment portal.
		/// </summary>
		/// <param name="order">OrderInfo</param>
		/// <param name="transactions">Order transactions stored from preceding calls <see cref="PreparePayment"/> or <see cref="CheckPaymentStatus"/></param>
		/// <returns>List of payment statuses. Does not have to be 1 to 1 with <see cref="transactions"/>.</returns>
		public PaymentInfo[] CheckPaymentStatus(Order order, string[] transactions)
		{
			return transactions.Select(
				transaction =>
				new PaymentInfo(
					order.Id,
					transaction,
					DecodePriceFromTransaction(transaction),
					order.CurrencyCode,
					DecodeStatusFromTransaction(transaction)))
				.ToArray();
		}

		#region UseTransactionInsteadOfActualPortal
		static string EncodePriceAndStatusIntoTransaction(decimal price, PaymentStatus status)
		{
			return string.Format("{0};{1};{2}", Guid.NewGuid(), price, status);
		}

		static decimal DecodePriceFromTransaction(string transaction)
		{
			var price = transaction.Split(';')[1];
			return decimal.Parse(price);
		}

		static PaymentStatus DecodeStatusFromTransaction(string transaction)
		{
			var status = transaction.Split(';')[2];
			return (PaymentStatus)Enum.Parse(typeof(PaymentStatus), status);
		}
		#endregion
	}
}
