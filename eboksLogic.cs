using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using RestSharp;
using DataFormat = RestSharp.DataFormat;
using MessageBox = System.Windows.MessageBox;

namespace MinEBoks
{
    public class Eboks
    {
        private const string BaseUrl = "https://rest.e-boks.dk/mobile/1/xml.svc/en-gb";

        private static readonly Session Session = new Session();


        private readonly object runningLock = new object();

        public void DownloadFromEBoks(IProgress<string> progress)
        {
            if (Monitor.TryEnter(runningLock))
            {
                try
                {
                    progress.Report("Kontrollerer for nye meddelelser");
                    GetSessionForAccountRest();
                    DownloadAll(progress);
                    progress.Report("Kontrol slut");

                    if (settings.opbyghentet)
                    {
                        settings.opbyghentet = false;
                        settings.Save();
                    }
                }
                finally
                {
                    Monitor.Exit(runningLock);
                }
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

            Session.DeviceId = settings.deviceid;

            request.AddHeader("X-EBOKS-AUTHENTICATE", GetAuthHeader(Session));
            request.AddHeader("Content-Type", "application/xml");
            request.AddHeader("Accept", "*/*");

            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                      "<Logon xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:eboks:mobile:1.0.0\">" +
                      "<App version=\"1.4.1\" os=\"iOS\" osVersion=\"9.0.0\" Device=\"iPhone\" />" +
                      "<User " +
                      "identity=\"" + settings.brugernavn + "\" " +
                      "identityType=\"P\" " +
                      "nationality=\"DK\" " +
                      "pincode=\"" + settings.password + "\"" +
                      "/>" +
                      "</Logon>";

            request.AddParameter("application/xml", xml, ParameterType.RequestBody);

            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (settings.Notification != null)
                {
                    settings.Notification.BalloonTipText = "Fejl ved hent: " + response.Content;
                    settings.Notification.ShowBalloonTip(5);
                }
                else
                {
                    MessageBox.Show("Fejl ved kald af EBoks : " + response.Content, "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    if (settings.IsHentet(messageId)) continue;

                    var messageName = message.Attribute("name").Value;
                    var format = message.Attribute("format").Value;
                    var afsender = message.Value;
                    var subject = message.Attribute("name").Value;
                    var modtaget = DateTime.Parse(message.Attribute("receivedDateTime").Value);

                    if (settings.opbyghentet)
                    {
                        progress.Report("Markeres som hentet " + afsender + " vedr. " + subject);
                    }
                    else
                    {
                        progress.Report("Henter meddelelse fra " + afsender + " vedr. " + subject);

                        // Hent vedhæftninger til besked
                        GetSessionForAccountRest();
                        var filename = MailContent(
                            Session.InternalUserId + "/0/mail/folder/" + folderid + "/message/" + messageId +
                            "/content", afsender.Trim() + " - " + messageName.Trim(), format, afsender, subject,
                            modtaget, progress);

                        settings.Notification.BalloonTipText = "Hentede " + subject + " fra eboks";
                        settings.Notification.BalloonTipClicked += (sender, e) =>
                        {
                            Process.Start(filename);
                        };
                        settings.Notification.ShowBalloonTip(10);

                    }

                    settings.AddHentet(messageId, afsender.Trim() + " - " + messageName.Trim());
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
            filename = settings.savepath + filename;

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

        private string MailContent(string url, string filename, string extension, string afsender, string subject,
            DateTime modtagetdato, IProgress<string> progress)
        {
            filename = GetContent(url, filename, extension, modtagetdato);
            if (filename == null)
                return null;

            if (settings.downloadonly)
                return filename;

            // Create a message and set up the recipients.
            var message = new MailMessage(
                settings.mailfrom,
                settings.mailto,
                subject,
                "")
            {
                From = new MailAddress(settings.mailfrom, afsender)
            };


            // Create  the file attachment for this e-mail message.
            var data = new Attachment(filename, MediaTypeNames.Application.Octet);
            //var disposition = data.ContentDisposition;
            // Add the file attachment to this e-mail message.
            message.Attachments.Add(data);

            //Send the message.
            var mailclient = new SmtpClient(settings.mailserver, settings.mailserverport)
            {
                Credentials =
                    new NetworkCredential(settings.mailserveruser, settings.mailserverpassword),
                EnableSsl = settings.mailserverssl
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

            return filename;
        }

        private string GetAuthHeader(Session session)
        {
            var date = DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss");

            string input =
                $"{settings.aktiveringskode}:{session.DeviceId}:P:{settings.brugernavn}:DK:{settings.password}:{date}";

            var challenge = Sha256Hash(input);
            challenge = Sha256Hash(challenge);

            return $"logon deviceid=\"{session.DeviceId}\",datetime=\"{date}\",challenge=\"{challenge}\"";
        }

        private string GetSessionHeader()
        {
            return
                $"deviceid={Session.DeviceId},nonce={Session.Nonce},sessionid={Session.SessionId},response={settings.response}";
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
    }
}