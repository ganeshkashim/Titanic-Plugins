using System;
using System.Activities;
using System.ServiceModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using System.Net;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Collections.Generic;

namespace TitanicPlugins
{
    public sealed class GetPrediction : CodeActivity
    {
        [Input("PassengerId")]
        public InArgument<String> PassengerId { get; set; }

        [Input("Name")]
        public InArgument<String> Name { get; set; }

        [Input("Age")]
        public InArgument<String> Age { get; set; }

        [Input("Embarked")]
        public InArgument<String> Embarked { get; set; }

        [Input("Cabin")]
        public InArgument<String> Cabin { get; set; }

        [Input("Fare")]
        public InArgument<String> Fare { get; set; }

        [Input("Parch")]
        public InArgument<String> Parch { get; set; }

        [Input("Pclass")]
        public InArgument<String> Pclass { get; set; }

        [Input("Survived")]
        public InArgument<String> Survived { get; set; }

        [Input("Ticket")]
        public InArgument<String> Ticket { get; set; }

        [Input("SibSp")]
        public InArgument<String> SibSp { get; set; }

        [Input("Sex")]
        public InArgument<String> Sex { get; set; }

        [Output("ScoredLabel")]
        public OutArgument<String> ScoredLabel { get; set; }

        [Output("ScoredProbability")]
        public OutArgument<String> ScoredProbability { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            try
            {
                JSONRequestResponse request = new JSONRequestResponse();
                request.InputObj = new TitanicPlugins.Input1();
                //request.inputObj2 = new Dictionary<string, string>() { };
                Input input = new Input();
                string[] columns = { "PassengerId", "Age", "Cabin", "Embarked", "Fare", "Name", "Parch", "Pclass", "SibSp", "Sex", "Ticket", "Survived" };
                object[] values = {PassengerId.Get(executionContext),Age.Get(executionContext),Cabin.Get(executionContext),Embarked.Get(executionContext),Fare.Get(executionContext),Name.Get(executionContext),
                Parch.Get(executionContext),Pclass.Get(executionContext),SibSp.Get(executionContext),Sex.Get(executionContext),Ticket.Get(executionContext),Survived.Get(executionContext)};
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

                ScoredLabel.Set(executionContext, myResponse.Results.Output1.Value.Values[0][9]);
                ScoredProbability.Set(executionContext, myResponse.Results.Output1.Value.Values[0][10]);
                


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
    }
}
