using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using System.ServiceModel;

namespace TitanicPlugins
{
    public class PreCreateTitanic : Plugin
    {
        public PreCreateTitanic() : base(typeof(PreCreateTitanic))
        {
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Create", "new_titanickaggle", new Action<LocalPluginContext>(ExecutePreCreateTitanic)));
        }
        protected void ExecutePreCreateTitanic(LocalPluginContext localContext)
        {

            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }
            IPluginExecutionContext context = localContext.PluginExecutionContext;
            IOrganizationService service = localContext.OrganizationService;
            ITracingService tracingService = localContext.TracingService;

            Entity titanicEntity = (Entity)context.InputParameters["Target"];

            try
            {
                JSONRequestResponse request = new JSONRequestResponse();
                request.InputObj = new TitanicPlugins.Input1();
                //request.inputObj2 = new Dictionary<string, string>() { };
                Input input = new Input();
                string[] columns = { "PassengerId", "Age", "Cabin", "Embarked", "Fare", "Name", "Parch", "Pclass", "SibSp", "Sex", "Ticket", "Survived" };
                object[] values = {  titanicEntity.Contains("new_passengerid") ? titanicEntity.GetAttributeValue<string>("new_passengerid") : "",
                                     titanicEntity.Contains("new_age") ? titanicEntity.GetAttributeValue<string>("new_age") : "",
                                     titanicEntity.Contains("new_cabin") ? titanicEntity.GetAttributeValue<string>("new_cabin") : "",
                                     titanicEntity.Contains("new_embarked") ? titanicEntity.GetAttributeValue<string>("new_embarked") : "",
                                     titanicEntity.Contains("new_fare") ? titanicEntity.GetAttributeValue<string>("new_fare") : "",
                                     titanicEntity.Contains("new_name") ? titanicEntity.GetAttributeValue<string>("new_name") : "",
                                     titanicEntity.Contains("new_parch") ? titanicEntity.GetAttributeValue<string>("new_parch") : "",
                                     titanicEntity.Contains("new_pclass") ? titanicEntity.GetAttributeValue<string>("new_pclass") : "",
                                     titanicEntity.Contains("new_sibsp") ? titanicEntity.GetAttributeValue<string>("new_sibsp") : "",
                                     titanicEntity.Contains("new_sex") ? titanicEntity.GetAttributeValue<string>("new_sex") : "",
                                     titanicEntity.Contains("new_ticket") ? titanicEntity.GetAttributeValue<string>("new_ticket") : "",
                                     titanicEntity.Contains("new_survived") ? titanicEntity.GetAttributeValue<string>("new_survived") : "" };
                input.Columns = columns;
                input.Values = new object[][] { values };
                request.InputObj.Inputs = new Input();
                request.InputObj.Inputs = input;

                System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(request.GetType());
                MemoryStream ms = new MemoryStream();
                serializer.WriteObject(ms, request);
                string jsonMsg = Encoding.Default.GetString(ms.ToArray());

                const string endpoint = "https://ussouthcentral.services.azureml.net/workspaces/92e7c840c83f4673ac594e767da8b538/services/e8b5c75d168345189225fcb5eab964d5/execute?api-version=2.0";
                const string apiKey = "PjAGXQN7aI8FhJ+bVPi7wFEt6QeUzLMTkx7FTkOcjxakVv2Fq4r8VNdnirlK2tBSIqp58sF4UiJ1tXT+l2eiTQ==";

                System.Net.WebRequest req = System.Net.WebRequest.Create(endpoint);
                req.ContentType = "application/json";
                req.Method = "POST";
                req.Headers.Add(string.Format("Authorization:Bearer {0}", apiKey));


                //create a stream
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(jsonMsg.ToString());
                req.ContentLength = bytes.Length;
                System.IO.Stream os = req.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
                os.Close();

                //get the response
                System.Net.WebResponse resp = req.GetResponse();

                Stream responseStream = CopyAndClose(resp.GetResponseStream());
                // Do something with the stream
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                String responseString = reader.ReadToEnd();
                tracingService.Trace("json response: {0}", responseString);

                responseStream.Position = 0;
                //deserialize the response to a myjsonresponse object
                JsonResponse myResponse = new JsonResponse();
                System.Runtime.Serialization.Json.DataContractJsonSerializer deserializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(myResponse.GetType());
                myResponse = deserializer.ReadObject(responseStream) as JsonResponse;

                tracingService.Trace("Scored Label- " + myResponse.Results.Output1.Value.Values[0][9]);
                tracingService.Trace("Scored Probablility- " + myResponse.Results.Output1.Value.Values[0][10]);


                titanicEntity.Attributes.Add("new_scoredlabel", myResponse.Results.Output1.Value.Values[0][9]);
                titanicEntity.Attributes.Add("new_scoredprobability", myResponse.Results.Output1.Value.Values[0][10]);
                

            }

            catch (WebException exception)
            {
                string str = string.Empty;
                if (exception.Response != null)
                {
                    using (StreamReader reader =
                        new StreamReader(exception.Response.GetResponseStream()))
                    {
                        str = reader.ReadToEnd();
                    }
                    exception.Response.Close();
                }
                if (exception.Status == WebExceptionStatus.Timeout)
                {
                    throw new InvalidPluginExecutionException(
                        "The timeout elapsed while attempting to issue the request.", exception);
                }
                throw new InvalidPluginExecutionException(String.Format(CultureInfo.InvariantCulture,
                    "A Web exception ocurred while attempting to issue the request. {0}: {1}",
                    exception.Message, str), exception);
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());

                // Handle the exception.
                throw;
            }
            catch (Exception e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());
                throw;
            }

            tracingService.Trace( ".Execute(), Correlation Id: {0}", context.CorrelationId);
        }


        private static Stream CopyAndClose(Stream inputStream)
        {
            const int readSize = 256;
            byte[] buffer = new byte[readSize];
            MemoryStream ms = new MemoryStream();

            int count = inputStream.Read(buffer, 0, readSize);
            while (count > 0)
            {
                ms.Write(buffer, 0, count);
                count = inputStream.Read(buffer, 0, readSize);
            }
            ms.Position = 0;
            inputStream.Close();
            return ms;
        }
        //static async Task InvokeRequestResponseService(Dictionary<string, string> parameters)
        //{

        //}
        //static void BuildScoreRequest(Entity entity)
        //{
        //    List<string> coulumnNames = new List<string>();
        //    List<string> values = new List<string>();
        //    foreach (var attribute in entity.Attributes)
        //    {
        //        coulumnNames.Add(attribute.Key);
        //        values.Add(entity.Attributes[attribute.Key].ToString());
        //    }
        //    var scoreRequest = new
        //    {

        //        Inputs = new Dictionary<string, StringTable>() {
        //                {
        //                    "input1",
        //                    new StringTable()
        //                    {
        //                        ColumnNames = coulumnNames.ToArray(),
        //                        Values = new string[,] {  { "0", "0", "0", "value", "value", "0", "0", "0", "value", "0", "value", "value" },  { "0", "0", "0", "value", "value", "0", "0", "0", "value", "0", "value", "value" },  }
        //                    }
        //                },
        //            },
        //        GlobalParameters = new Dictionary<string, string>()
        //        {
        //        }
        //    };


        //}

    }
}
