//-----------------------------------------------------------------------
// <copyright file="Update.cs" company="Equifax Inc">
//     Copyright 2025 Equifax. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Kount.Ris
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Net.Http;
    using Kount.Ris.Authentication;
    using Microsoft.Extensions.Http;

    /// <summary>
    /// Update class. A bunch of setters for sending transaction update <br/>
    /// data to a Kount RIS server.
    /// <b>Author:</b> Kount <a>custserv@kount.com</a>;<br/>
    /// <b>Version:</b> 8.0.0. <br/>
    /// <b>Copyright:</b> 2025 Equifax<br/>
    /// </summary>
    public class Update : Kount.Ris.Request
    {
        /// <summary>
        /// Refund chargeback type refund
        /// </summary>
        private const char RfcbR = 'R';

        /// <summary>
        /// Refund chargeback type chargeback
        /// </summary>
        private const char RfcbC = 'C';

        

        /// <summary>
        /// Constructor. Sets the mode to 'U' by default.
        /// Use setMode(char) to change it.
        /// </summary>
        public Update(ILogger logger = null) : base(true, null)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor. Sets the mode to 'U' by default.
        /// Use setMode(char) to change it.
        /// </summary>
        /// <param name="checkConfiguration">If is true: will check config file if 
        /// `Ris.Url`, 
        /// `Ris.MerchantId`, 
        /// `Ris.Config.Key` and `Ris.Connect.Timeout` are set.</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(bool checkConfiguration, ILogger logger = null) : base(checkConfiguration, logger)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor. Sets the mode to 'U' by default.
        /// Use setMode(char) to change it.
        /// </summary>
        /// <param name="checkConfiguration">If is true: will check config file if 
        /// `Ris.Url`, 
        /// `Ris.MerchantId`, 
        /// `Ris.Config.Key` and `Ris.Connect.Timeout` are set.</param>
        /// <param name="configuration">Configuration class with raw values</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(bool checkConfiguration, Configuration configuration, ILogger logger = null) : base(checkConfiguration, configuration, logger)        
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor with authentication provider for dependency injection.
        /// Sets the mode to 'U' by default.
        /// </summary>
        /// <param name="authenticationProvider">Authentication provider for handling tokens</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(IAuthenticationProvider authenticationProvider, ILogger logger = null) 
            : base(true, Configuration.FromAppSettings(), authenticationProvider, logger)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor with configuration and authentication provider for dependency injection.
        /// Sets the mode to 'U' by default.
        /// </summary>
        /// <param name="checkConfiguration">If is true: will check config file if 
        /// `Ris.Url`, `Ris.MerchantId`, `Ris.Config.Key` and `Ris.Connect.Timeout` are set.</param>
        /// <param name="configuration">Configuration class with raw values</param>
        /// <param name="authenticationProvider">Authentication provider for handling tokens</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(bool checkConfiguration, Configuration configuration, 
                      IAuthenticationProvider authenticationProvider, ILogger logger = null) 
            : base(checkConfiguration, configuration, authenticationProvider, logger)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor with IHttpClientFactory for dependency injection scenarios.
        /// Sets the mode to 'U' by default.
        /// Enables proper HttpClient lifecycle management and connection pooling.
        /// </summary>
        /// <param name="httpClientFactory">HttpClient factory for creating clients with proper lifecycle management</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(IHttpClientFactory httpClientFactory, ILogger logger = null) 
            : base(true, Configuration.FromAppSettings(), null, httpClientFactory, logger)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor with configuration and IHttpClientFactory for dependency injection scenarios.
        /// Sets the mode to 'U' by default.
        /// Enables proper HttpClient lifecycle management and connection pooling.
        /// </summary>
        /// <param name="configuration">Configuration class with raw values</param>
        /// <param name="httpClientFactory">HttpClient factory for creating clients with proper lifecycle management</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(Configuration configuration, IHttpClientFactory httpClientFactory, ILogger logger = null) 
            : base(true, configuration, null, httpClientFactory, logger)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Constructor with configuration, authentication provider, and IHttpClientFactory for full dependency injection control.
        /// Sets the mode to 'U' by default.
        /// </summary>
        /// <param name="checkConfiguration">If is true: will check config file if 
        /// `Ris.Url`, `Ris.MerchantId`, `Ris.Config.Key` and `Ris.Connect.Timeout` are set.</param>
        /// <param name="configuration">Configuration class with raw values</param>
        /// <param name="authenticationProvider">Authentication provider for handling tokens</param>
        /// <param name="httpClientFactory">HttpClient factory for creating clients with proper lifecycle management</param>
        /// <param name="logger">ILogger object for logging output</param>
        public Update(bool checkConfiguration, Configuration configuration, 
                      IAuthenticationProvider authenticationProvider, 
                      IHttpClientFactory httpClientFactory, ILogger logger = null) 
            : base(checkConfiguration, configuration, authenticationProvider, httpClientFactory, logger)
        {
            this.SetMode(Enums.UpdateTypes.ModeU);
        }

        /// <summary>
        /// Set the mode of the update.
        /// </summary>
        /// <param name="mode">Set U or X</param>
        /// <exception cref="Kount.Ris.IllegalArgumentException">Thrown if
        /// parameter is an invalid mode.</exception>
        protected override void SetMode(char mode)
        {
            if (((char)Enums.UpdateTypes.ModeU != mode)
                && ((char)Enums.UpdateTypes.ModeX != mode))
            {
                throw new Kount.Ris.IllegalArgumentException(
                    "Invalid RIS update mode " + mode);
            }

            this.Data["MODE"] = mode;
        }

        /// <summary>
        /// Set the original associated transaction id generated by Kount
        /// </summary>
        /// <param name="transactionId">Transaction id.</param>
        public void SetTransactionId(string transactionId)
        {
            this.Data["TRAN"] = this.SafeGet(transactionId);
        }

        /// <summary>
        /// Set if this transaction ended up being a refund or chargeback.
        /// </summary>
        /// <param name="rfcb">Set R or C.</param>
        public void SetRefundChargeback(char rfcb)
        {
            if ((RfcbR != rfcb) && (RfcbC != rfcb))
            {
                throw new Kount.Ris.IllegalArgumentException(
                    "Invalid RIS refund charge back value " + rfcb);
            }

            this.Data["RFCB"] = rfcb;
        }

        /// <summary>
        /// Set the paypal Id
        /// </summary>
        /// <param name="paypalId">Set paypal Id</param>
        [Obsolete("version 4.1.0 - 2010. Use Kount.Ris.Update.SetPayPalPayment() instead")]
        public void SetPayPalId(string paypalId)
        {
            string message = "The method " +
                "Kount.Ris.Update.SetPaypalId() is obsolete. " +
                "Use Kount.Ris.Update.SetPaypalPayment(bool) instead.";
            logger.LogInformation(message);
            this.SetPayment(Enums.PaymentTypes.Paypal, paypalId);
        }
    }
}