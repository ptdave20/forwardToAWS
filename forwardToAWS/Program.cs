using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GmailQuickstart
{

    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        static string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailCompose, GmailService.Scope.GmailSend, GmailService.Scope.GmailModify };
        static string ApplicationName = "Gmail API .NET Quickstart";

        public static string Base64UrlEncode(string input)
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        static byte[] Base64UrlDecode(string arg)
        {
            string s = arg;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default:
                    throw new System.Exception(
             "Illegal base64url string!");
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        static void Main(string[] args)
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            var msgs = service.Users.Messages.List("me");
            msgs.IncludeSpamTrash = true;
            msgs.Q = "is:unread in:spam";
            msgs.MaxResults = 200;
            var result = msgs.Execute();
            do
            {

                foreach (var msg in result.Messages)
                {
                    var messageReq = service.Users.Messages.Get("me", msg.Id);
                    messageReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                    var messageResp = messageReq.Execute();
                    var raw = Encoding.ASCII.GetString(Base64UrlDecode(messageResp.Raw));
                    if (raw.Contains("amazonaws.com"))
                    {
                        //Console.WriteLine(raw);
                        Message sMsg = new Message()
                        {
                            Raw = Base64UrlEncode("To:abuse@amazonaws.com\r\nFrom:ptdave20@gmail.com\r\n" +
                            "Content-Type: text/plain; charset=us-ascii\r\n\r\n" + raw)
                        };
                        service.Users.Messages.Send(sMsg, "me").Execute();
                    }
                    service.Users.Messages.Trash("me", messageResp.Id).Execute();
                }
                if (string.IsNullOrEmpty(result.NextPageToken))
                    break;
                msgs.PageToken = result.NextPageToken;
                result = msgs.Execute();
            } while (true);

            //// List labels.
            //IList<Label> labels = request.Execute().Labels;
            //Console.WriteLine("Labels:");
            //if (labels != null && labels.Count > 0)
            //{
            //    foreach (var labelItem in labels)
            //    {
            //        Console.WriteLine("{0}", labelItem.Name);
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("No labels found.");
            //}
            Console.Read();
        }
    }
}