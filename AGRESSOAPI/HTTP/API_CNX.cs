using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Configuration;
using APIAGRESSO.Models;
using Newtonsoft.Json;


namespace APIAGRESSO.HTTP
{
    class API_CNX
    {
        private static string url = ConfigurationSettings.AppSettings["UAPI_AGR_Url"];
        private static Token token;
        private static string login = ConfigurationSettings.AppSettings["API_AGR_Login"].ToString();
        private static string password = ConfigurationSettings.AppSettings["API_AGR_Password"].ToString();

        public API_CNX()
        {
            loginacess lg = new loginacess();
            lg.login = login;
            lg.password = password;
            token = this.GETTOKEN("POST", lg.toJson(), "/api/gettoken");
        }

        private void checkValideToken(Token token_)
        {
            if( token_.dtexp <= DateTime.Now )
            {
                Console.WriteLine("RESET EXPIRED TOKEN !");
                loginacess lg = new loginacess();
                lg.login = login;
                lg.password = password;
                token = this.GETTOKEN("POST", lg.toJson(), "/api/gettoken");
            }
        }


        public string HTTPAGRESSO(string Methode, string dataJson, string action)
        {

            try
            {
                checkValideToken(token);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url + action);
                //-- Desable SSL
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + token.token);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream())){streamWriter.Write(dataJson);}
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {return streamReader.ReadToEnd();}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("URL : " + url + action);
                return "-1";
            }
        }

        public Token GETTOKEN(string Methode, string dataJson, string action)
        {
          
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url + action);
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(login + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpWebRequest.Headers.Add("Authorization", "Basic " + val);
                

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream())){streamWriter.Write(dataJson);}

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Console.WriteLine("Token : " + result);
                    return JsonConvert.DeserializeObject<Token>(result);

                }
            }
            catch (Exception ex)
            {
                return new Token();
            }
        }
    }
}

