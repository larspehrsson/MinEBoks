using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using RestSharp;

namespace MinEBoks
{
    public partial class MainWindow
    {
        private const string BaseUrl = "https://rest.e-boks.dk/mobile/1/xml.svc/en-gb";
        private static Session _session = new Session();
        private List<string> Hentet = new List<string>();

        private void LoadHentetList()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\hentet.txt"))
                {
                    while (sr.Peek() >= 0)
                    {
                        Hentet.Add(sr.ReadLine());
                    }
                }

            }
            catch
            {
                Hentet = new List<string>();
            }
        }

        private void SaveHentetList(string id)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\hentet.txt"))
                    sw.WriteLine(id);
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(1000);
                using (StreamWriter sw = File.AppendText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\hentet.txt"))
                    sw.WriteLine(id);
            }
        }

        public void GetSessionForAccountRest(Account account)
        {
            var client = new RestClient(BaseUrl)
            {
                UserAgent = "eboks/35 CFNetwork/672.1.15 Darwin/14.0.0"
            };

            var request = new RestRequest("/session", Method.PUT)
            {
                RequestFormat = DataFormat.Xml
            };

            _session.DeviceId = account.DeviceId;

            request.AddHeader("X-EBOKS-AUTHENTICATE", GetAuthHeader(account, _session));
            request.AddHeader("Content-Type", "application/xml");
            request.AddHeader("Accept", "*/*");

            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                      "<Logon xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:eboks:mobile:1.0.0\">" +
                          "<App version=\"1.4.1\" os=\"iOS\" osVersion=\"9.0.0\" Device=\"iPhone\" />" +
                          "<User " +
                              "identity=\"" + account.UserId + "\" " +
                              "identityType=\"P\" " +
                              "nationality=\"DK\" " +
                              "pincode=\"" + account.Password + "\"" +
                          "/>" +
                      "</Logon>";

            request.AddParameter("application/xml", xml, ParameterType.RequestBody);

            var response = client.Execute(request);

            var sessionid =
                response.Headers.Where(c => c.Name == "X-EBOKS-AUTHENTICATE")
                    .Select(c => c.Value)
                    .FirstOrDefault()
                    .ToString()
                    .Split(',');

            _session.SessionId = sessionid[0].Substring(11, sessionid[0].Length - 12);
            _session.Nonce = sessionid[1].Substring(8, sessionid[1].Length - 9);

            var doc = RemoveAllNameSpaces(response.Content);

            _session.Name = doc.XPathSelectElement("Session/User").Attribute("name").Value;
            _session.InternalUserId = doc.XPathSelectElement("Session/User").Attribute("userId").Value;
        }

        
        public void DownloadAll(Account account, IProgress<string> progress)
        {

            // Henter liste over foldere
            var xdoc = getxml(account, _session.InternalUserId + "/0/mail/folders");
            var queryFolders = (from t in xdoc.Descendants("FolderInfo") where t.Attribute("id") != null select t).ToList();

            // Traverser alle folderne
            foreach (var folder in queryFolders)
            {
                var folderid = folder.Attribute("id").Value;
                //var foldername = folder.Attribute("name").Value;

                // Hent liste over beskeder i hver folder
                GetSessionForAccountRest(account);
                var messages = getxml(account, _session.InternalUserId + "/0/mail/folder/" + folderid + "?skip=0&take=100");

                // Traverser hver besked og hent vedhæftninger
                var queryMessages = (from t in messages.Descendants("MessageInfo") where t.Attribute("id") != null select t).ToList();
                foreach (var message in queryMessages)
                {
                    var messageId = message.Attribute("id").Value;

                    // Kontroller hvis allerede hentet
                    if (Hentet.Contains(messageId))
                        continue;

                    var messageName = message.Attribute("name").Value;
                    var format = message.Attribute("format").Value;
                    var afsender = message.Value;
                    var subject = message.Attribute("name").Value;
                    var modtaget = DateTime.Parse(message.Attribute("receivedDateTime").Value);

                    if (Properties.Settings.Default.opbyghentet)
                    {
                        progress.Report("Markeres som hentet " + afsender + " vedr. " + subject);

                    }
                    else
                    {
                        progress.Report("Henter meddelelse fra " + afsender + " vedr. " + subject);

                        // Hent vedhæftninger til besked
                        GetSessionForAccountRest(account);
                        mailContent(account,
                            _session.InternalUserId + "/0/mail/folder/" + folderid + "/message/" + messageId +
                            "/content", messageName + " - " + messageId + "." + format, afsender, subject, modtaget, progress);
                    }
                    Hentet.Add(messageId);
                    SaveHentetList(messageId);
                }
            }

        }

        public XDocument getxml(Account account, string url)
        {
            var client = new RestClient(BaseUrl)
            {
                UserAgent = "eboks/35 CFNetwork/672.1.15 Darwin/14.0.0"
            };

            var request = new RestRequest(url, Method.GET);
            request.AddHeader("X-EBOKS-AUTHENTICATE", GetSessionHeader(account));
            request.AddHeader("Accept", "*/*");

            var response = client.Execute(request);
            var responsedoc = RemoveAllNameSpaces(response.Content);

            return responsedoc;
        }


        public XDocument RemoveAllNameSpaces(string content)
        {
            // TODO: Remove this ugly replace
            var responsedoc = XDocument.Parse(content.Replace("xmlns=\"urn:eboks:mobile:1.0.0\"", ""));
            // Remove all namespaces from document
            responsedoc.Descendants().Attributes().Where(a => a.IsNamespaceDeclaration).Remove();

            return responsedoc;
        }

        public string getContent(Account account, string url, string filename, DateTime modtagetdato)
        {
            filename = Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c, '_'));
            filename = Properties.Settings.Default.savepath + filename;

            if (File.Exists(filename))
                return null;

            var client = new RestClient(BaseUrl)
            {
                UserAgent = "eboks/35 CFNetwork/672.1.15 Darwin/14.0.0"
            };

            var request = new RestRequest(url, Method.GET);
            request.AddHeader("X-EBOKS-AUTHENTICATE", GetSessionHeader(account));
            request.AddHeader("Accept", "*/*");

            var filedata = client.DownloadData(request);
            File.WriteAllBytes(filename, filedata);

            File.SetCreationTime(filename, modtagetdato);
            File.SetLastWriteTime(filename, modtagetdato);
            File.SetLastAccessTime(filename, modtagetdato);

            return filename;
        }

        public bool mailContent(Account account, string url, string filename, string afsender, string subject, DateTime modtagetdato, IProgress<string> progress)
        {
            filename = getContent(account, url, filename, modtagetdato);
            if (filename == null)
                return true;

            if (Properties.Settings.Default.downloadonly)
                return true;

            // Create a message and set up the recipients.
            var message = new MailMessage(
                Properties.Settings.Default.mailfrom,
                Properties.Settings.Default.mailto,
                subject,
                "")
            {
                From = new MailAddress(Properties.Settings.Default.mailfrom, afsender)
            };


            // Create  the file attachment for this e-mail message.
            var data = new Attachment(filename, MediaTypeNames.Application.Octet);
            var disposition = data.ContentDisposition;
            // Add the file attachment to this e-mail message.
            message.Attachments.Add(data);

            //Send the message.
            var mailclient = new SmtpClient(Properties.Settings.Default.mailserver, Properties.Settings.Default.mailserverport)
            {
                Credentials = new NetworkCredential(Properties.Settings.Default.mailserveruser, Properties.Settings.Default.mailserverpassword),
                EnableSsl = Properties.Settings.Default.mailserverssl,
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

            return true;
        }

        private string GetAuthHeader(Account account, Session session)
        {
            var date = DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss");

            string input =
                $"{account.ActivationCode}:{session.DeviceId}:P:{account.UserId}:DK:{account.Password}:{date}";

            var challenge = Sha256Hash(input);
            challenge = Sha256Hash(challenge);

            return $"logon deviceid=\"{session.DeviceId}\",datetime=\"{date}\",challenge=\"{challenge}\"";
        }

        private string GetSessionHeader(Account account)
        {
            return $"deviceid={_session.DeviceId},nonce={_session.Nonce},sessionid={_session.SessionId},response={account.response}";
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