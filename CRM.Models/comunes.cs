using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CRM.Models
{
    public static class comunes
    {
        public static string GetToken(string laip)
        {

            sesion sesion = new sesion();
            string urlPath = "https://riews.reinfoempresa.com:8443";
            string request2 = urlPath + "/RIEWS/webapi/PublicServices/authenticationDefaultW";
            string SecretKey = "eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJSZWN1cnNvcyBJbmZvcm3DoXRpY29zIEVtcHJlc2FyaWFsZXMsIFMuTC4iLCJpYXQiOjE1Nzc5NTgzNjYsImV4cCI6MTYwOTU4MDc2NywibmJmIjoxNTc3OTU4MzY2LCJpc3MiOiJhZHJpYW4iLCJlbnQiOiI0IiwiaW5zIjoiMSIsInVzYyI6IjMiLCJzbnUiOiI5OTk5LTg4ODgtMTEiLCJpcHMiOiJOVEV1TnpjdU1UTTNMakU0Tnc9PSIsImV4ZSI6IjExLjAuNSsxMC1wb3N0LVVidW50dS0wdWJ1bnR1MS4xMTguMDQiLCJsYW4iOiJzcGEiLCJqdGkiOiIwNTY0YzlmZS02NGI2LTRiNzAtYWZiZS04YmZhMDk1Y2U3NjkifQ.kkjBAlvDdNQrWC_8DCp5pEbMDBdHSBpRsmEZEKUm16bwn_45cktl3eudhOp7OxptqwgAt19prQowdKL3W3Zenw";
            WebRequest webRequest = WebRequest.Create(request2);
            //definimos el tipo de webrequest al que llamaremos
            webRequest.Method = "POST";
            //definimos content
            webRequest.ContentType = "application/json; charset=utf-8";
            //cargamos los datos a enviar
            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                string json = "{\"tokensite\":\"" + SecretKey/*System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes()) */+ "\"" +
                    ",\"ipbase64\":\"" + laip + "\",\"language\":\"SPA\"}";
                streamWriter.Write(json);
            }
            var httpResponse = (HttpWebResponse)webRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var resultado = streamReader.ReadToEnd();
                sesion = JsonConvert.DeserializeObject<sesion>(resultado);
            }
            return sesion.activeToken;
        }

       

    }
}
