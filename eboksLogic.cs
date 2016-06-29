using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;
using MinEBoks.Properties;
using RestSharp;
using DataFormat = RestSharp.DataFormat;

namespace MinEBoks
{
    internal class Session
    {
        public string Name { get; set; }

        public string InternalUserId { get; set; }

        public string DeviceId { get; set; }

        public string SessionId { get; set; }

        public string Nonce { get; set; }
    }

    public class Eboks
    {
        private const string BaseUrl = "https://rest.e-boks.dk/mobile/1/xml.svc/en-gb";

        private static readonly Session Session = new Session();

        private static readonly string purpose = "NwA3ADUAMQ!AxASDkAMbwAzADcA0";
        //private List<string> _hentet = new List<string>();

        public void DownloadFromEBoks(IProgress<string> progress)
        {
            progress.Report("Kontrollerer for nye meddelelser");
            GetSessionForAccountRest();
            DownloadAll(progress);
            progress.Report("Kontrol slut");

            if (Settings.Default.opbyghentet)
            {
                Settings.Default.opbyghentet = false;
                Settings.Default.Save();
            }
        }


        public bool GetSessionForAccountRest()
        {
            var client = new RestClient(BaseUrl)
            {
                UserAgent = "eboks/35 CFNetwork/672.1.15 Darwin/14.0.0"
            };

            var request = new RestRequest("/session", Method.PUT)
            {
                RequestFormat = DataFormat.Xml
            };

            Session.DeviceId = Settings.Default.deviceid;

            request.AddHeader("X-EBOKS-AUTHENTICATE", GetAuthHeader(Session));
            request.AddHeader("Content-Type", "application/xml");
            request.AddHeader("Accept", "*/*");

            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                      "<Logon xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:eboks:mobile:1.0.0\">" +
                      "<App version=\"1.4.1\" os=\"iOS\" osVersion=\"9.0.0\" Device=\"iPhone\" />" +
                      "<User " +
                      "identity=\"" + Unprotect(Settings.Default.brugernavn) + "\" " +
                      "identityType=\"P\" " +
                      "nationality=\"DK\" " +
                      "pincode=\"" + Unprotect(Settings.Default.password) + "\"" +
                      "/>" +
                      "</Logon>";

            request.AddParameter("application/xml", xml, ParameterType.RequestBody);

            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                MessageBox.Show("Fejl ved kald af EBoks : " + response.Content, "Fejl", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            var firstOrDefault = response.Headers.Where(c => c.Name == "X-EBOKS-AUTHENTICATE")
                .Select(c => c.Value)
                .FirstOrDefault();

            if (firstOrDefault != null)
            {
                var sessionid =
                    firstOrDefault
                        .ToString()
                        .Split(',');

                Session.SessionId = sessionid[0].Substring(11, sessionid[0].Length - 12);
                Session.Nonce = sessionid[1].Substring(8, sessionid[1].Length - 9);
            }

            var doc = RemoveAllNameSpaces(response.Content);

            Session.Name = doc.XPathSelectElement("Session/User").Attribute("name").Value;
            Session.InternalUserId = doc.XPathSelectElement("Session/User").Attribute("userId").Value;

            return !string.IsNullOrEmpty(Session.Name);
        }


        private void DownloadAll(IProgress<string> progress)
        {
            // Henter liste over foldere
            var xdoc = GetXML(Session.InternalUserId + "/0/mail/folders");
            var queryFolders =
                (from t in xdoc.Descendants("FolderInfo") where t.Attribute("id") != null select t).ToList();

            // Traverser alle folderne
            foreach (var folder in queryFolders)
            {
                var folderid = folder.Attribute("id").Value;
                //var foldername = folder.Attribute("name").Value;

                // Hent liste over beskeder i hver folder
                GetSessionForAccountRest();
                var messages = GetXML(Session.InternalUserId + "/0/mail/folder/" + folderid + "?skip=0&take=100");

                // Traverser hver besked og hent vedhæftninger
                var queryMessages =
                    (from t in messages.Descendants("MessageInfo") where t.Attribute("id") != null select t).ToList();
                foreach (var message in queryMessages)
                {
                    var messageId = message.Attribute("id").Value;

                    // Kontroller hvis allerede hentet
                    if (Settings.Default.idhentet.Contains(messageId)) continue;

                    var messageName = message.Attribute("name").Value;
                    var format = message.Attribute("format").Value;
                    var afsender = message.Value;
                    var subject = message.Attribute("name").Value;
                    var modtaget = DateTime.Parse(message.Attribute("receivedDateTime").Value);

                    if (Settings.Default.opbyghentet)
                    {
                        progress.Report("Markeres som hentet " + afsender + " vedr. " + subject);
                    }
                    else
                    {
                        progress.Report("Henter meddelelse fra " + afsender + " vedr. " + subject);

                        // Hent vedhæftninger til besked
                        GetSessionForAccountRest();
                        MailContent(
                            Session.InternalUserId + "/0/mail/folder/" + folderid + "/message/" + messageId +
                            "/content", afsender.Trim() + " - " + messageName.Trim(), format, afsender, subject,
                            modtaget, progress);
                    }

                    Settings.Default.idhentet.Add(messageId);
                    Settings.Default.Save();
                }
            }
        }

        private XDocument GetXML(string url)
        {
            var client = new RestClient(BaseUrl)
            {
                UserAgent = "eboks/35 CFNetwork/672.1.15 Darwin/14.0.0"
            };

            var request = new RestRequest(url, Method.GET);
            request.AddHeader("X-EBOKS-AUTHENTICATE", GetSessionHeader());
            request.AddHeader("Accept", "*/*");

            var response = client.Execute(request);
            var responsedoc = RemoveAllNameSpaces(response.Content);

            return responsedoc;
        }


        private XDocument RemoveAllNameSpaces(string content)
        {
            // TODO: Remove this ugly replace
            var responsedoc = XDocument.Parse(content.Replace("xmlns=\"urn:eboks:mobile:1.0.0\"", ""));
            // Remove all namespaces from document
            responsedoc.Descendants().Attributes().Where(a => a.IsNamespaceDeclaration).Remove();

            return responsedoc;
        }

        private string GetContent(string url, string filename, string extension, DateTime modtagetdato)
        {
            extension = extension.ToLower();

            filename = Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c, '_'));
            filename = Settings.Default.savepath + filename;

            if (File.Exists(filename + "." + extension))
            {
                var i = 0;
                string tmpfilename;
                do
                {
                    i += 1;
                    tmpfilename = filename + "(" + i + ")." + extension;
                } while (File.Exists(tmpfilename));
                filename = tmpfilename;
            }
            else
                filename += "." + extension;

            var client = new RestClient(BaseUrl)
            {
                UserAgent = "eboks/35 CFNetwork/672.1.15 Darwin/14.0.0"
            };

            var request = new RestRequest(url, Method.GET);
            request.AddHeader("X-EBOKS-AUTHENTICATE", GetSessionHeader());
            request.AddHeader("Accept", "*/*");

            var filedata = client.DownloadData(request);
            File.WriteAllBytes(filename, filedata);

            File.SetCreationTime(filename, modtagetdato);
            File.SetLastWriteTime(filename, modtagetdato);
            File.SetLastAccessTime(filename, modtagetdato);

            return filename;
        }

        private void MailContent(string url, string filename, string extension, string afsender, string subject,
            DateTime modtagetdato, IProgress<string> progress)
        {
            filename = GetContent(url, filename, extension, modtagetdato);
            if (filename == null)
                return;

            if (Settings.Default.downloadonly)
                return;

            // Create a message and set up the recipients.
            var message = new MailMessage(
                Settings.Default.mailfrom,
                Settings.Default.mailto,
                subject,
                "")
            {
                From = new MailAddress(Settings.Default.mailfrom, afsender)
            };


            // Create  the file attachment for this e-mail message.
            var data = new Attachment(filename, MediaTypeNames.Application.Octet);
            //var disposition = data.ContentDisposition;
            // Add the file attachment to this e-mail message.
            message.Attachments.Add(data);

            //Send the message.
            var mailclient = new SmtpClient(Settings.Default.mailserver, Settings.Default.mailserverport)
            {
                Credentials =
                    new NetworkCredential(Settings.Default.mailserveruser, Settings.Default.mailserverpassword),
                EnableSsl = Settings.Default.mailserverssl
            };

            try
            {
                mailclient.Send(message);
            }
            catch (Exception ex)
            {
                progress.Report($"Exception caught in CreateMessageWithAttachment(): {ex}");
            }

            data.Dispose();
        }

        private string GetAuthHeader(Session session)
        {
            var date = DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss");

            string input =
                $"{Unprotect(Settings.Default.aktiveringskode)}:{session.DeviceId}:P:{Unprotect(Settings.Default.brugernavn)}:DK:{Unprotect(Settings.Default.password)}:{date}";

            var challenge = Sha256Hash(input);
            challenge = Sha256Hash(challenge);

            return $"logon deviceid=\"{session.DeviceId}\",datetime=\"{date}\",challenge=\"{challenge}\"";
        }

        private string GetSessionHeader()
        {
            return
                $"deviceid={Session.DeviceId},nonce={Session.Nonce},sessionid={Session.SessionId},response={Settings.Default.response}";
        }

        private string Sha256Hash(string value)
        {
            var sb = new StringBuilder();

            using (var hasher = SHA256.Create())
            {
                var result = hasher.ComputeHash(Encoding.UTF8.GetBytes(value));
                foreach (var b in result)
                    sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string Protect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var myUnprotectedBytes = Encoding.UTF8.GetBytes(text);
            var encodedValue = MachineKey.Protect(myUnprotectedBytes, purpose);

            return Convert.ToBase64String(encodedValue);
        }

        public static string Unprotect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                var stream = Convert.FromBase64String(text);
                var decodedValue = MachineKey.Unprotect(stream, purpose);
                return Encoding.UTF8.GetString(decodedValue);
            }
            catch (Exception)
            {
                return text;
            }
        }
    }
}