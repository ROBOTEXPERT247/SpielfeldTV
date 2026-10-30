using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SpielfeldTV {
public class Camera {
 public string Id = Guid.NewGuid().ToString();
 public string Name = "Neue Kamera", Host = "", User = "", Password = "", Path = "/onvif1";
 public int Port = 554;
 public string Service = "";
 public bool Tcp = true;
 public string Backend = "Auto";
 public int DvripPort = 34567;
 public string Url() { return "rtsp://" + (User.Length == 0 ? "" : Uri.EscapeDataString(User) + ":" + Uri.EscapeDataString(Password) + "@") + Host + ":" + Port + Path.Replace("{user}",Uri.EscapeDataString(User)).Replace("{password}",Uri.EscapeDataString(Password)); }
 public override string ToString() { return Name; }
 public Camera Copy() { return (Camera)MemberwiseClone(); }
 public void Validate() {
  IPAddress ip; Uri uri;
  if (String.IsNullOrWhiteSpace(Name)) throw new Exception("Bitte einen Kameranamen eintragen.");
  if (!IPAddress.TryParse(Host, out ip) && Uri.CheckHostName(Host) != UriHostNameType.Dns) throw new Exception("Bitte eine gültige IP-Adresse oder einen Hostnamen eingeben.");
  if (Host.Contains(":") || Port < 1 || Port > 65535) throw new Exception("IPv4/Hostname und Port 1–65535 verwenden.");
  if (!Path.StartsWith("/") || Path.Contains("://") || Path.Any(Char.IsWhiteSpace)) throw new Exception("Der Stream-Pfad muss mit / beginnen und darf keine Leerzeichen enthalten.");
  if (!Uri.TryCreate(Url(), UriKind.Absolute, out uri)) throw new Exception("Die Stream-Adresse ist ungültig.");
 }
}
public class ViewPage {
 public string Name = "Spielfeld", Screen = "", ScreenKey = "";
 public int Rows = 2, Columns = 4;
 public List<string> Slots = new List<string>();
 public void Normalize() { Rows = Math.Max(1, Math.Min(5, Rows)); Columns = Math.Max(1, Math.Min(5, Columns)); while(Slots.Count < 25) Slots.Add(""); }
 public override string ToString() { return Name; }
}
public class Settings {
 public List<Camera> Cameras = new List<Camera>();
 public List<ViewPage> Pages = new List<ViewPage>();
 public List<ViewerCaption> ViewerCaptions = new List<ViewerCaption>();
 public List<string> ActivePages = new List<string>();
 public bool AutoOpen = false;
 public List<string> DisabledScreens = new List<string>();
 public int Cache = 400;
 public bool Hardware = true;
 public bool Multicam = true;
 public static Settings Defaults() {
  var s = new Settings(); var screens = Screen.AllScreens;
  for(int i=0;i<3;i++) s.Pages.Add(new ViewPage { Name="TV " + (i+1), Rows=2, Columns=i==2?2:4, Screen=screens[Math.Min(i,screens.Length-1)].DeviceName });
  foreach(var p in s.Pages) p.Normalize(); return s;
 }
}
public static class Store {
 public static string FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpielfeldTV", "settings.dat");
 public static byte[] Encode(Settings s) { using(var m=new MemoryStream()) { new XmlSerializer(typeof(Settings)).Serialize(m,s); return ProtectedData.Protect(m.ToArray(),null,DataProtectionScope.CurrentUser); } }
 public static Settings Decode(byte[] b) { var raw=ProtectedData.Unprotect(b,null,DataProtectionScope.CurrentUser); using(var m=new MemoryStream(raw)) return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(m); }
 public static void Save(Settings s) {
  Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FileName)); var temp=FileName+".tmp";
  File.WriteAllBytes(temp,Encode(s)); if(File.Exists(FileName)) File.Replace(temp,FileName,null); else File.Move(temp,FileName);
 }
 public static Settings Load() { if(!File.Exists(FileName)) return Settings.Defaults(); var s=Decode(File.ReadAllBytes(FileName)); foreach(var p in s.Pages)p.Normalize(); return s; }
}
public static class UI {
 public static Color Bg=Color.FromArgb(16,22,31), Card=Color.FromArgb(26,35,47), Ink=Color.FromArgb(229,237,246), Accent=Color.FromArgb(57,214,172);
 public static void Style(Form f) { f.BackColor=Bg; f.ForeColor=Ink; f.Font=new Font("Segoe UI",10); f.StartPosition=FormStartPosition.CenterScreen; f.AutoScaleMode=AutoScaleMode.Dpi; }
 public static Button Button(string text, EventHandler click) { var b=new Button {Text=text,AutoSize=true,Height=36,Padding=new Padding(10,4,10,4),FlatStyle=FlatStyle.Flat,BackColor=Card,ForeColor=Ink,Margin=new Padding(4)}; b.FlatAppearance.BorderColor=Color.FromArgb(66,86,105); b.Click+=click; return b; }
 public static Label Label(string text) { return new Label {Text=text,AutoSize=true,ForeColor=Ink,Margin=new Padding(5,10,5,5)}; }
 public static FlowLayoutPanel Bar() { return new FlowLayoutPanel {Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8),BackColor=Card,WrapContents=true}; }
 public static ComboBox Combo() { return new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,Width=220,Margin=new Padding(5),Height=28}; }
 public static void Error(Exception ex) { MessageBox.Show(ex.Message,"SpielfeldTV",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
 public static DataGridView Grid() { var g=new DataGridView {Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=Bg,BorderStyle=BorderStyle.None,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,EnableHeadersVisualStyles=false,ColumnHeadersHeight=40,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing}; g.DefaultCellStyle.BackColor=Card; g.DefaultCellStyle.ForeColor=Ink; g.DefaultCellStyle.SelectionBackColor=Color.FromArgb(39,91,94); g.DefaultCellStyle.SelectionForeColor=Color.White; g.ColumnHeadersDefaultCellStyle.BackColor=Bg; g.ColumnHeadersDefaultCellStyle.ForeColor=Ink; g.RowTemplate.Height=34; return g; }
}
public class Found {
 public string Host, Kind, Name;
 public string Service = "";
 public int Port=554;
}
public static class Search {
 public static IEnumerable<IPAddress> LocalAddresses() { return NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up && n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Where(a=>a.Address.AddressFamily==AddressFamily.InterNetwork).Select(a=>a.Address).Distinct(); }
 public static string Probe(string id) { return "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\" xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:dn=\"http://www.onvif.org/ver10/network/wsdl\"><s:Header><a:MessageID>urn:uuid:"+id+"</a:MessageID><a:To>urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To><a:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action></s:Header><s:Body><d:Probe><d:Types>dn:NetworkVideoTransmitter</d:Types></d:Probe></s:Body></s:Envelope>"; }
 public static List<Found> Parse(byte[] data, string fallback) {
  var result=new List<Found>();
  using(var sr=new StringReader(Encoding.UTF8.GetString(data))) using(var reader=XmlReader.Create(sr,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=65536})) {
   var doc=XDocument.Load(reader);
   foreach(var match in doc.Descendants().Where(x=>x.Name.LocalName=="ProbeMatch")) {
    var addresses=match.Descendants().FirstOrDefault(x=>x.Name.LocalName=="XAddrs");
    var scopes=match.Descendants().FirstOrDefault(x=>x.Name.LocalName=="Scopes");
    string name="ONVIF-Kamera";
    if(scopes!=null) { var scope=scopes.Value.Split(' ').FirstOrDefault(v=>v.Contains("/name/")); if(scope!=null) name=Uri.UnescapeDataString(scope.Substring(scope.IndexOf("/name/")+6)); }
    var host=fallback;string service="";
    if(addresses!=null) foreach(var a in addresses.Value.Split((char[])null,StringSplitOptions.RemoveEmptyEntries)) { Uri uri; IPAddress ip; if(Uri.TryCreate(a,UriKind.Absolute,out uri) && IPAddress.TryParse(uri.Host,out ip) && ip.AddressFamily==AddressFamily.InterNetwork && (uri.Scheme=="http"||uri.Scheme=="https")) { host=uri.Host; service=uri.AbsoluteUri; break; } }
    result.Add(new Found {Host=host,Service=service,Kind="ONVIF erkannt – Stream noch prüfen",Name=name});
   }
  } return result;
 }
 public static async Task<List<Found>> Discover(CancellationToken token) {
  var jobs=LocalAddresses().Select(ip=>Task.Run(()=> {
   var list=new List<Found>();
   using(var udp=new UdpClient(new IPEndPoint(ip,0))) {
    udp.Client.ReceiveTimeout=300;
    udp.Client.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.MulticastTimeToLive,2);
    var msg=Encoding.UTF8.GetBytes(Probe(Guid.NewGuid().ToString())); var dest=new IPEndPoint(IPAddress.Parse("239.255.255.250"),3702);
    udp.Send(msg,msg.Length,dest); udp.Send(msg,msg.Length,dest);
    var end=DateTime.UtcNow.AddSeconds(4);
    while(DateTime.UtcNow<end && !token.IsCancellationRequested) { try { IPEndPoint from=new IPEndPoint(IPAddress.Any,0); var bytes=udp.Receive(ref from); list.AddRange(Parse(bytes,from.Address.ToString())); } catch(SocketException) {} catch(XmlException) {} catch(UriFormatException) {} }
   } return list;
  },token)).ToArray();
  var all=await Task.WhenAll(jobs); return all.SelectMany(x=>x).GroupBy(x=>x.Host).Select(g=>g.First()).ToList();
 }
 public static uint Number(string s) { IPAddress ip; if(!IPAddress.TryParse(s,out ip)||ip.AddressFamily!=AddressFamily.InterNetwork) throw new Exception("Bitte gültige IPv4-Adressen eingeben."); var b=ip.GetAddressBytes(); return ((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3]; }
 public static string Address(uint n) { return new IPAddress(new byte[]{(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n}).ToString(); }
 public static async Task<bool> PortOpen(string host,int port,CancellationToken token) {
  using(var client=new TcpClient()) { try { var connect=client.ConnectAsync(host,port); var done=await Task.WhenAny(connect,Task.Delay(650,token)); if(done!=connect || token.IsCancellationRequested)return false; await connect; return client.Connected; } catch(SocketException){return false;} catch(ObjectDisposedException){return false;} }
 }
 public static async Task<List<Found>> Scan(string start,string finish,int port,CancellationToken token,IProgress<int> progress) {
  uint first=Number(start),last=Number(finish); if(last<first || (ulong)last-first>1023) throw new Exception("Bitte einen Bereich von höchstens 1024 Adressen wählen.");
  var found=new List<Found>(); int count=0;
  using(var gate=new SemaphoreSlim(32)) {
   var jobs=Enumerable.Range(0,(int)(last-first+1)).Select(async offset=> { await gate.WaitAsync(token); try { token.ThrowIfCancellationRequested(); string ip=Address(first+(uint)offset); if(await PortOpen(ip,port,token)) lock(found) found.Add(new Found {Host=ip,Port=port,Name="Kamera "+ip,Kind="Port "+port+" offen – Kamera/Stream unbestätigt"}); progress.Report(Interlocked.Increment(ref count)); } finally {gate.Release();} }).ToArray();
   await Task.WhenAll(jobs);
  } return found.OrderBy(x=>Number(x.Host)).ToList();
 }
}
public class DiscoveryForm : Form {
 DataGridView grid=UI.Grid(); TextBox from=new TextBox {Width=165},to=new TextBox {Width=165}; NumericUpDown port=new NumericUpDown {Minimum=1,Maximum=65535,Value=554,Width=75}; Label status=UI.Label("ONVIF-Suche oder IP-Bereich wählen. Keine Anmeldung wird ausprobiert."); Button onvif,scan,add; CancellationTokenSource cancel; List<Found> found=new List<Found>();
 public List<Found> Selected=new List<Found>();
 public DiscoveryForm() {
  Text="Kameras im Netzwerk suchen"; Size=new Size(1030,620); MinimumSize=new Size(860,480); UI.Style(this);
  grid.MultiSelect=true; grid.Columns.Add("host","IP-Adresse"); grid.Columns.Add("name","Gerätename"); grid.Columns.Add("kind","Erkennung");
  var bar=UI.Bar(); onvif=UI.Button("ONVIF automatisch suchen",async (s,e)=>await Run(false)); scan=UI.Button("IP-Bereich prüfen",async(s,e)=>await Run(true));
  bar.Controls.Add(onvif); bar.Controls.Add(UI.Label("Von"));bar.Controls.Add(from);bar.Controls.Add(UI.Label("Bis"));bar.Controls.Add(to);bar.Controls.Add(UI.Label("Port"));bar.Controls.Add(port);bar.Controls.Add(scan);
  var bottom=UI.Bar();bottom.Dock=DockStyle.Bottom;
  add=UI.Button("Auswahl hinzufügen",(s,e)=>{ foreach(DataGridViewRow row in grid.SelectedRows) Selected.Add((Found)row.Tag); if(Selected.Count>0) {DialogResult=DialogResult.OK;Close();} });
  bottom.Controls.Add(add);bottom.Controls.Add(UI.Button("Suche abbrechen",(s,e)=>{if(cancel!=null)cancel.Cancel();}));bottom.Controls.Add(status);
  Controls.Add(grid);Controls.Add(bar);Controls.Add(bottom);
  var local=Search.LocalAddresses().FirstOrDefault(); string prefix=local==null?"192.168.1.":local.ToString().Substring(0,local.ToString().LastIndexOf('.')+1);from.Text=prefix+"1";to.Text=prefix+"254";
  FormClosing+=(s,e)=>{if(cancel!=null)cancel.Cancel();};
 }
 async Task Run(bool range) {
  onvif.Enabled=scan.Enabled=add.Enabled=false; cancel=new CancellationTokenSource(); status.Text="Suche läuft …";
  try { var result=range?await Search.Scan(from.Text.Trim(),to.Text.Trim(),(int)port.Value,cancel.Token,new Progress<int>(n=>{if(!IsDisposed)status.Text=n+" Adressen geprüft …";})):await Search.Discover(cancel.Token);
   if(IsDisposed)return;
   foreach(var f in result) {var old=found.FirstOrDefault(x=>x.Host==f.Host);if(old==null)found.Add(f);else if(f.Service.Length>0){old.Service=f.Service;old.Kind=f.Kind;old.Name=f.Name;}}
   grid.Rows.Clear(); foreach(var f in found) {int i=grid.Rows.Add(f.Host,f.Name,f.Kind);grid.Rows[i].Tag=f;}
   status.Text=found.Count+" Geräte/Anschlüsse gefunden. Strg-Klick für Mehrfachauswahl.";
   if(found.Count==0)status.Text="Kein Fund. IP-Bereich, Windows-Firewall, Gast-WLAN und ONVIF-Einstellung prüfen.";
  } catch(OperationCanceledException) {if(!IsDisposed)status.Text="Suche abgebrochen.";} catch(Exception ex){if(!IsDisposed)UI.Error(ex);} finally {if(!IsDisposed)onvif.Enabled=scan.Enabled=add.Enabled=true;cancel.Dispose();cancel=null;}
 }
}
public class StreamProfile {
 public string Name, Path, Service;
 public int Port;
 public override string ToString(){return Name;}
}
public static class Onvif {
 static readonly XNamespace Media="http://www.onvif.org/ver10/media/wsdl",Device="http://www.onvif.org/ver10/device/wsdl",Schema="http://www.onvif.org/ver10/schema";
 static string Escape(string s){return System.Security.SecurityElement.Escape(s);}
 static string Envelope(string body,Camera camera){
  byte[] nonce=new byte[20];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(nonce);
  string created=DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ",System.Globalization.CultureInfo.InvariantCulture);byte[] tail=Encoding.UTF8.GetBytes(created+camera.Password);byte[] input=nonce.Concat(tail).ToArray();string digest;using(var sha=SHA1.Create())digest=Convert.ToBase64String(sha.ComputeHash(input));
  string security=camera.User.Length==0?"":"<wsse:Security s:mustUnderstand=\"1\" xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\"><wsse:UsernameToken><wsse:Username>"+Escape(camera.User)+"</wsse:Username><wsse:Password Type=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest\">"+digest+"</wsse:Password><wsse:Nonce EncodingType=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary\">"+Convert.ToBase64String(nonce)+"</wsse:Nonce><wsu:Created>"+created+"</wsu:Created></wsse:UsernameToken></wsse:Security>";
  return "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Header>"+security+"</s:Header><s:Body>"+body+"</s:Body></s:Envelope>";
 }
 static Uri Endpoint(string text,Camera camera){Uri uri;if(!Uri.TryCreate(text,UriKind.Absolute,out uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))throw new Exception("Kein gültiger ONVIF-Endpunkt.");if(!String.Equals(uri.Host,camera.Host,StringComparison.OrdinalIgnoreCase))throw new Exception("ONVIF-Endpunkt gehört zu einer anderen Adresse. Bitte Kamera neu suchen.");if(uri.UserInfo.Length>0)throw new Exception("ONVIF-Adresse bitte ohne eingebettete Zugangsdaten verwenden.");return uri;}
 static async Task<XDocument> Send(string endpoint,string action,string body,Camera camera,CancellationToken token){
  var uri=Endpoint(endpoint,camera);var request=(HttpWebRequest)WebRequest.Create(uri);request.Method="POST";request.Proxy=null;request.ContentType="application/soap+xml; charset=utf-8; action=\""+action+"\"";request.Headers["SOAPAction"]="\""+action+"\"";request.Credentials=new NetworkCredential(camera.User,camera.Password);request.AllowAutoRedirect=false;request.KeepAlive=false;
  byte[] payload=Encoding.UTF8.GetBytes(Envelope(body,camera));request.ContentLength=payload.Length;
  using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token)){timeout.CancelAfter(5500);using(timeout.Token.Register(()=>request.Abort())){
   try {using(var output=await request.GetRequestStreamAsync())await output.WriteAsync(payload,0,payload.Length,timeout.Token);
    using(var response=(HttpWebResponse)await request.GetResponseAsync())using(var stream=response.GetResponseStream())using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1048576})){
     var doc=XDocument.Load(reader);if(doc.Descendants().Any(x=>x.Name.LocalName=="Fault")){bool auth=doc.Descendants().Any(x=>x.Value.IndexOf("NotAuthorized",StringComparison.OrdinalIgnoreCase)>=0);throw new Exception(auth?"ONVIF lehnt die Anmeldung ab. Lokalen Gerätebenutzer, Gerätepasswort und Kamera-Uhrzeit prüfen.":"ONVIF meldet einen Fehler bei dieser Anfrage.");}return doc;
    }
   }catch(WebException ex){token.ThrowIfCancellationRequested();var response=ex.Response as HttpWebResponse;int status=response==null?0:(int)response.StatusCode;if(response!=null)response.Dispose();if(status==401||status==403)throw new UnauthorizedAccessException("ONVIF-Anmeldung abgelehnt. Lokale Geräte-Zugangsdaten und Kamera-Uhrzeit prüfen.");throw new Exception(status==0?"ONVIF antwortet nicht rechtzeitig auf diesem Anschluss.":"ONVIF antwortet mit HTTP "+status+".");}
  }}
 }
 public static async Task<List<StreamProfile>> Read(Camera camera,CancellationToken token,IProgress<string> progress){
  var endpoints=new[]{camera.Service,"http://"+camera.Host+":8899/onvif/device_service","http://"+camera.Host+"/onvif/device_service","http://"+camera.Host+":8000/onvif/device_service"}.Where(x=>!String.IsNullOrWhiteSpace(x)).Distinct().ToArray();var errors=new List<string>();
  foreach(var endpoint in endpoints){token.ThrowIfCancellationRequested();try{
   var uri=Endpoint(endpoint,camera);progress.Report("ONVIF auf Anschluss "+uri.Port+" prüfen …");
   var caps=await Send(endpoint,Device.NamespaceName+"/GetCapabilities","<GetCapabilities xmlns=\""+Device+"\"><Category>Media</Category></GetCapabilities>",camera,token);
   var media=caps.Descendants().FirstOrDefault(x=>x.Name.LocalName=="Media");var addr=media==null?null:media.Descendants().FirstOrDefault(x=>x.Name.LocalName=="XAddr");if(addr==null)throw new Exception("Kamera nennt keinen ONVIF-Mediendienst.");Endpoint(addr.Value,camera);
   progress.Report("Kameraprofile auslesen …");var profiles=await Send(addr.Value,Media.NamespaceName+"/GetProfiles","<GetProfiles xmlns=\""+Media+"\"/>",camera,token);var result=new List<StreamProfile>();
   foreach(var profile in profiles.Descendants().Where(x=>x.Name.LocalName=="Profiles").Take(8)){
    var attribute=profile.Attribute("token");if(attribute==null)continue;var name=profile.Elements().FirstOrDefault(x=>x.Name.LocalName=="Name");
    var config=profile.Descendants().FirstOrDefault(x=>x.Name.LocalName=="VideoEncoderConfiguration");if(config==null)continue;
    string codec=Value(config,"Encoding"),width=Value(config,"Width"),height=Value(config,"Height");
    var body=new XElement(Media+"GetStreamUri",new XElement(Media+"StreamSetup",new XElement(Schema+"Stream","RTP-Unicast"),new XElement(Schema+"Transport",new XElement(Schema+"Protocol","RTSP"))),new XElement(Media+"ProfileToken",attribute.Value));
    var info=await Send(addr.Value,Media.NamespaceName+"/GetStreamUri",body.ToString(SaveOptions.DisableFormatting),camera,token);var location=info.Descendants().FirstOrDefault(x=>x.Name.LocalName=="Uri");Uri stream;if(location==null||!Uri.TryCreate(location.Value,UriKind.Absolute,out stream)||stream.Scheme!="rtsp")continue;
    if(!String.Equals(stream.Host,camera.Host,StringComparison.OrdinalIgnoreCase)&&stream.Host!="0.0.0.0")continue;
    result.Add(new StreamProfile{Name=(name==null?"Videoprofil":name.Value)+" · "+codec+" "+width+"×"+height,Path=stream.PathAndQuery,Port=stream.Port>0?stream.Port:554,Service=endpoint});
   }
   if(result.Count>0)return result;throw new Exception("Keine passende RTSP-Adresse für ein Videoprofil erhalten.");
  }catch(UnauthorizedAccessException){throw;}catch(OperationCanceledException){throw;}catch(Exception ex){errors.Add(ex.Message);if(ex.Message.Contains("Anmeldung"))throw;}}
  throw new Exception("Keine Stream-Adresse ermittelt.\r\n"+String.Join("\r\n",errors.Distinct())+"\r\nONVIF in der Kamera aktivieren oder Kamera erneut über die Netzwerksuche hinzufügen.");
 }
 static string Value(XElement e,string name){var child=e.Descendants().FirstOrDefault(x=>x.Name.LocalName==name);return child==null?"":child.Value;}
}
public class RtspResult {public int Code;public string Challenge="",Body="";public bool Video;public string Summary;}
public static class Rtsp {
 static string Quote(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","").Replace("\n","")+"\"";}
 static string Hash(string s,string algorithm){using(HashAlgorithm h=algorithm.StartsWith("SHA-256",StringComparison.OrdinalIgnoreCase)?(HashAlgorithm)SHA256.Create():MD5.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
 public static string Auth(string challenge,Camera c,string uri){
  if(challenge.StartsWith("Basic",StringComparison.OrdinalIgnoreCase))return "Basic "+Convert.ToBase64String(Encoding.UTF8.GetBytes(c.User+":"+c.Password));
  if(!challenge.StartsWith("Digest",StringComparison.OrdinalIgnoreCase))throw new Exception("Diese Anmeldemethode wird von der Diagnose nicht unterstützt. Livebild trotzdem testen.");
  var fields=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(Match m in Regex.Matches(challenge.Substring(6),"([A-Za-z0-9_-]+)\\s*=\\s*(?:\"([^\"]*)\"|([^,\\s]+))"))fields[m.Groups[1].Value]=m.Groups[2].Success?m.Groups[2].Value:m.Groups[3].Value;
  if(!fields.ContainsKey("realm")||!fields.ContainsKey("nonce"))throw new Exception("Unvollständige RTSP-Anmeldeantwort.");string algorithm=fields.ContainsKey("algorithm")?fields["algorithm"]:"MD5";
  if(!new[]{"MD5","MD5-sess","SHA-256","SHA-256-sess"}.Contains(algorithm,StringComparer.OrdinalIgnoreCase))throw new Exception("Digest-Verfahren der Kamera wird in der Diagnose nicht unterstützt.");
  string qop="";if(fields.ContainsKey("qop")){if(!fields["qop"].Split(',').Any(x=>x.Trim()=="auth"))throw new Exception("Digest-Variante der Kamera wird in der Diagnose nicht unterstützt.");qop="auth";}
  string nonce=fields["nonce"],realm=fields["realm"],cnonce=Guid.NewGuid().ToString("N"),nc="00000001",ha1=Hash(c.User+":"+realm+":"+c.Password,algorithm);if(algorithm.EndsWith("-sess",StringComparison.OrdinalIgnoreCase))ha1=Hash(ha1+":"+nonce+":"+cnonce,algorithm);
  string ha2=Hash("DESCRIBE:"+uri,algorithm),response=Hash(ha1+":"+nonce+":"+(qop.Length>0?nc+":"+cnonce+":"+qop+":":"")+ha2,algorithm);
  return "Digest username="+Quote(c.User)+", realm="+Quote(realm)+", nonce="+Quote(nonce)+", uri="+Quote(uri)+", response="+Quote(response)+", algorithm="+algorithm+(qop.Length>0?", qop=auth, nc="+nc+", cnonce="+Quote(cnonce):algorithm.EndsWith("-sess",StringComparison.OrdinalIgnoreCase)?", cnonce="+Quote(cnonce):"")+(fields.ContainsKey("opaque")?", opaque="+Quote(fields["opaque"]):"");
 }
 static async Task<RtspResult> Exchange(NetworkStream stream,string uri,string auth,int sequence,CancellationToken token){
  string text="DESCRIBE "+uri+" RTSP/1.0\r\nCSeq: "+sequence+"\r\nUser-Agent: SpielfeldTV/0.2\r\nAccept: application/sdp\r\n"+(auth.Length>0?"Authorization: "+auth+"\r\n":"")+"\r\n";byte[] bytes=Encoding.UTF8.GetBytes(text);await stream.WriteAsync(bytes,0,bytes.Length,token);
  var header=new List<byte>();var one=new byte[1];while(header.Count<32768){int n=await stream.ReadAsync(one,0,1,token);if(n==0)throw new Exception("Kamera hat die RTSP-Verbindung geschlossen.");header.Add(one[0]);int k=header.Count;if(k>=4&&header[k-4]==13&&header[k-3]==10&&header[k-2]==13&&header[k-1]==10)break;}
  if(header.Count>=32768)throw new Exception("Ungewöhnlich große RTSP-Antwort.");string[] lines=Encoding.UTF8.GetString(header.ToArray()).Split(new[]{"\r\n"},StringSplitOptions.None);int code;var first=lines[0].Split(' ');if(first.Length<2||!first[0].StartsWith("RTSP/")||!Int32.TryParse(first[1],out code))throw new Exception("Der Anschluss antwortet nicht mit RTSP.");
  int size=0;string challenge="";foreach(string line in lines.Skip(1)){int colon=line.IndexOf(':');if(colon<0)continue;string key=line.Substring(0,colon),value=line.Substring(colon+1).Trim();if(key.Equals("Content-Length",StringComparison.OrdinalIgnoreCase))Int32.TryParse(value,out size);if(key.Equals("WWW-Authenticate",StringComparison.OrdinalIgnoreCase))challenge=value;}
  if(size<0||size>262144)throw new Exception("Ungewöhnlich große Stream-Beschreibung.");byte[] body=new byte[size];int offset=0;while(offset<size){int n=await stream.ReadAsync(body,offset,size-offset,token);if(n==0)throw new Exception("Unvollständige Stream-Beschreibung.");offset+=n;}string sdp=Encoding.UTF8.GetString(body);return new RtspResult{Code=code,Challenge=challenge,Body=sdp,Video=Regex.IsMatch(sdp,@"(?m)^m=video\s")};
 }
 public static async Task<RtspResult> Check(Camera camera,CancellationToken token){
  using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token))using(var client=new TcpClient()){
   timeout.CancelAfter(7000);using(timeout.Token.Register(()=>client.Close())){try{
    await client.ConnectAsync(camera.Host,camera.Port);var builder=new UriBuilder(camera.Url()){UserName="",Password=""};string uri=builder.Uri.AbsoluteUri;var stream=client.GetStream();var result=await Exchange(stream,uri,"",1,timeout.Token);
    if(result.Code==401&&camera.User.Length>0)result=await Exchange(stream,uri,Auth(result.Challenge,camera,uri),2,timeout.Token);
    result.Summary=result.Code==200?(result.Video?"RTSP antwortet: Videostream vorhanden. Wenn das Bild schwarz bleibt, Hardwaredecodierung ausschalten oder TCP/UDP wechseln.":"RTSP antwortet, nennt aber keinen Videostream."):result.Code==401||result.Code==403?"RTSP-Anmeldung abgelehnt. Lokalen Gerätebenutzer und Gerätepasswort prüfen.":result.Code==404?"Kamera erreichbar, aber dieser RTSP-Pfad wurde nicht gefunden.":"Kamera antwortet mit RTSP "+result.Code+". Pfad oder Kamerafreigabe prüfen.";return result;
   }catch(SocketException){token.ThrowIfCancellationRequested();throw new Exception("Keine RTSP-Verbindung zu IP/Port. Kamera-IP, Anschluss und Erreichbarkeit aus diesem WLAN prüfen. Ein Gastnetz kann Kamerazugriff verhindern.");}catch(ObjectDisposedException){token.ThrowIfCancellationRequested();throw new Exception("Zeitüberschreitung bei der RTSP-Prüfung.");}catch(IOException){token.ThrowIfCancellationRequested();throw new Exception("RTSP-Verbindung abgebrochen oder Zeitüberschreitung.");}catch(OperationCanceledException){token.ThrowIfCancellationRequested();throw new Exception("Zeitüberschreitung bei der RTSP-Prüfung.");}
   }
  }
 }
}
public class ProfileForm : Form {
 public StreamProfile Selected;ListBox list=new ListBox{Dock=DockStyle.Fill};
 public ProfileForm(List<StreamProfile> profiles){Text="Kamerastream auswählen";Size=new Size(680,330);UI.Style(this);foreach(var p in profiles)list.Items.Add(p);if(list.Items.Count>0)list.SelectedIndex=0;var tip=UI.Label("Für viele Kameras den kleineren Stream wählen. Danach Livebild testen.");tip.Dock=DockStyle.Top;var bar=UI.Bar();bar.Dock=DockStyle.Bottom;bar.Controls.Add(UI.Button("Ausgewählten Stream übernehmen",(a,b)=>{Selected=list.SelectedItem as StreamProfile;if(Selected!=null){DialogResult=DialogResult.OK;Close();}}));Controls.Add(list);Controls.Add(tip);Controls.Add(bar);}
}
public static class Vlc {
 public static bool Diagnostics;
 [StructLayout(LayoutKind.Sequential)] public struct Stats {public int Read;public float InputRate;public int DemuxRead;public float DemuxRate;public int Corrupt,Discontinuity,DecodedVideo,DecodedAudio,Displayed,Lost,PlayedAudio,LostAudio,SentPackets,SentBytes;public float SendRate;}
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] public static extern bool libvlc_media_get_stats(IntPtr m,out Stats stats);
 public static IntPtr Instance; public static string Failure="";
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool SetDllDirectory(string path);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr libvlc_new(int n,IntPtr args);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr libvlc_media_new_location(IntPtr i,byte[] url);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_media_add_option(IntPtr m,byte[] option);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_media_release(IntPtr m);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr libvlc_media_player_new_from_media(IntPtr m);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_media_player_set_hwnd(IntPtr p,IntPtr h);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int libvlc_media_player_play(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_media_player_stop(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_media_player_release(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int libvlc_media_player_get_state(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_audio_set_mute(IntPtr p,int mute);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_video_set_mouse_input(IntPtr p,uint on);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void libvlc_video_set_key_input(IntPtr p,uint on);
 public static byte[] Utf(string s) { return Encoding.UTF8.GetBytes(s+"\0"); }
 static readonly object initGate=new object(),asyncGate=new object();static Task<bool> initialization;
 public static Task<bool> InitAsync(){lock(asyncGate){if(initialization==null||initialization.IsFaulted||initialization.IsCanceled||(initialization.Status==TaskStatus.RanToCompletion&&!initialization.Result))initialization=Task.Run(()=>Init());return initialization;}}
 public static bool Init(){lock(initGate)return InitCore();}
 static bool InitCore() {
  if(Instance!=IntPtr.Zero)return true;
  string path=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"vlc");
  if(!File.Exists(System.IO.Path.Combine(path,"libvlc.dll")))path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"VideoLAN","VLC");
  if(!File.Exists(System.IO.Path.Combine(path,"libvlc.dll"))) {Failure="VLC fehlt. Bitte VLC 3 (64 Bit) von videolan.org installieren und SpielfeldTV neu starten. Verwaltung und Demo funktionieren auch ohne VLC.";return false;}
  try { SetDllDirectory(path); Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH",System.IO.Path.Combine(path,"plugins"));
   var args=Diagnostics?new[]{"--no-video-title-show","--no-audio","--no-osd","--drop-late-frames","--no-skip-frames","--file-logging","--logfile="+System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"vlc-test.log"),"--verbose=2"}:new[]{"--no-video-title-show","--no-audio","--quiet","--no-osd","--drop-late-frames","--no-skip-frames"};
   var ptrs=args.Select(Marshal.StringToHGlobalAnsi).ToArray(); var argv=Marshal.AllocHGlobal(IntPtr.Size*ptrs.Length);
   try {Marshal.Copy(ptrs,0,argv,ptrs.Length);Instance=libvlc_new(ptrs.Length,argv);} finally {foreach(var p in ptrs)Marshal.FreeHGlobal(p);Marshal.FreeHGlobal(argv);}
   if(Instance==IntPtr.Zero)throw new Exception("VLC konnte nicht gestartet werden.");return true;
  } catch(Exception) {Failure="VLC konnte nicht geladen werden. Benötigt: VLC 3, 64 Bit (x64), mit plugins-Ordner.";return false;}
 }
}
public static class NativeBridge {
 public static bool Credentials(Camera camera,out string user,out string hash){var m=Regex.Match(Uri.UnescapeDataString(camera.Path),@"user=(.*?)_password=([A-Za-z0-9]{8})_channel=");user=m.Success?m.Groups[1].Value:"";hash=m.Success?m.Groups[2].Value:"";return m.Success;}
 public static bool Use(Camera camera){string user,hash;return camera.Backend=="XM"||(camera.Backend=="Auto"&&Credentials(camera,out user,out hash));}
 public static void Stop(){}
}
public class RawCameraSource : IDisposable {
 public static int FrameRate(byte value){int rate=value&63;return rate>0?rate:12;}
 TcpClient camera,viewer;TcpListener listener;TaskCompletionSource<RawCameraSource> ready=new TaskCompletionSource<RawCameraSource>();volatile bool closed;uint session,sequence;CameraPackets buffer=new CameraPackets();Camera settings;string user,hash;public string Url,Demux;public int Fps;long received,frameCount;public long Frames {get{return Interlocked.Read(ref frameCount);}} public long Received {get{return Interlocked.Read(ref received);}}
 RawCameraSource(Camera c,string u,string h){settings=c.Copy();user=u;hash=h;}
 public static async Task<RawCameraSource> Open(Camera c){string user,hash;if(!NativeBridge.Credentials(c,out user,out hash)){var profiles=await Onvif.Read(c,CancellationToken.None,new Progress<string>(s=>{}));var copy=c.Copy();copy.Path=profiles[0].Path;if(!NativeBridge.Credentials(copy,out user,out hash))throw new Exception("Kamera-Anmeldewert fehlt. Stream automatisch ermitteln.");}var source=new RawCameraSource(c,user,hash);var running=Task.Factory.StartNew(()=>source.Run(),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);try{return await source.ready.Task;}catch{source.Dispose();throw;}}
 static void Read(Stream stream,byte[] bytes){int pos=0;while(pos<bytes.Length){int n=stream.Read(bytes,pos,bytes.Length-pos);if(n==0)throw new IOException();pos+=n;}}
 byte[] Chunk(){var h=new byte[20];Read(camera.GetStream(),h);if(h[0]!=255)throw new IOException();session=BitConverter.ToUInt32(h,4);int length=BitConverter.ToInt32(h,16);if(length<0||length>8388608)throw new IOException();var data=new byte[length];Read(camera.GetStream(),data);return data;}
 byte[] Command(ushort command,string body){byte[] data=Encoding.UTF8.GetBytes(body+"\n\0"),header=new byte[20];header[0]=255;BitConverter.GetBytes(session).CopyTo(header,4);BitConverter.GetBytes(sequence++).CopyTo(header,8);BitConverter.GetBytes(command).CopyTo(header,14);BitConverter.GetBytes(data.Length).CopyTo(header,16);camera.GetStream().Write(header,0,20);camera.GetStream().Write(data,0,data.Length);return Chunk();}
 void Success(byte[] data){var m=Regex.Match(Encoding.UTF8.GetString(data),"\"Ret\"\\s*:\\s*(\\d+)");if(!m.Success||m.Groups[1].Value!="100")throw new Exception("Kamera lehnt die native Videoanfrage ab.");}
 byte[] Packet(){while(!closed){byte[] packet;if(buffer.TryRead(out packet))return packet;buffer.AddRange(Chunk());}throw new IOException();}
 void Run(){try{
  camera=new TcpClient();camera.NoDelay=true;var connect=camera.ConnectAsync(settings.Host,settings.DvripPort);if(!connect.Wait(5000))throw new IOException();camera.ReceiveTimeout=10000;camera.SendTimeout=5000;
  string login="{\"EncryptType\":\"MD5\",\"LoginType\":\"DVRIP-Web\",\"PassWord\":\""+hash+"\",\"UserName\":\""+user.Replace("\\","\\\\").Replace("\"","\\\"")+"\"}";Success(Command(1000,login));
  string kind=Regex.IsMatch(settings.Path,@"stream=1(?:\D|$)")?"Extra1":"Main";string monitor="{\"Name\":\"OPMonitor\",\"SessionID\":\"0x"+session.ToString("X8")+"\",\"OPMonitor\":{\"Action\":\"Claim\",\"Parameter\":{\"Channel\":0,\"CombinMode\":\"NONE\",\"StreamType\":\""+kind+"\",\"TransMode\":\"TCP\"}}}";Success(Command(1413,monitor));var start=Command(1410,monitor.Replace("Claim","Start"));if(start.Length>=4&&start[0]==0&&start[1]==0&&start[2]==1)buffer.AddRange(start);
  byte[] first;var firstDeadline=DateTime.UtcNow.AddSeconds(15);do{if(DateTime.UtcNow>firstDeadline)throw new IOException();first=Packet();}while(first[3]!=252&&first[3]!=254);int codec=first[4];Demux=(codec==2||codec==18)?"h264":(codec==3||codec==19||codec==67||codec==83)?"hevc":null;if(Demux==null)throw new Exception("Nicht unterstütztes Kamera-Videoformat.");Fps=FrameRate(first[5]);listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Url="tcp://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port;ready.TrySetResult(this);viewer=listener.AcceptTcpClient();viewer.NoDelay=true;viewer.SendTimeout=10000;var output=viewer.GetStream();output.Write(first,16,first.Length-16);Interlocked.Add(ref received,first.Length-16);Interlocked.Increment(ref frameCount);
  while(!closed){var frame=Packet();if(frame[3]==252||frame[3]==254){output.Write(frame,16,frame.Length-16);Interlocked.Add(ref received,frame.Length-16);Interlocked.Increment(ref frameCount);}else if(frame[3]==253){output.Write(frame,8,frame.Length-8);Interlocked.Add(ref received,frame.Length-8);Interlocked.Increment(ref frameCount);}}
 }catch(Exception){ready.TrySetException(new Exception("Direkter Kamera-Videokanal konnte nicht geöffnet werden oder wurde unterbrochen."));}finally{Dispose();}}
 public void Dispose(){closed=true;if(listener!=null)try{listener.Stop();}catch{}if(camera!=null)try{camera.Close();}catch{}if(viewer!=null)try{viewer.Close();}catch{}}
}
public class PlaybackHealth {
 public DateTime Started,StateSince,LastVideo;public int State=-1,Frames;public bool HadVideo;
 public PlaybackHealth(DateTime now){Started=StateSince=LastVideo=now;}
 public string Observe(int state,int frames,DateTime now){
  if(state!=State){State=state;StateSince=now;}
  if(frames>0&&frames!=Frames){Frames=frames;LastVideo=now;HadVideo=true;}
  if(state==7)return "VLC meldet einen Medien-/Verbindungsfehler";
  if(state==6)return "VLC meldet das Wiedergabeende";
  if(state==5&&(now-Started).TotalSeconds>3)return "RTSP-Wiedergabe wurde gestoppt";
  if(HadVideo&&(now-LastVideo).TotalSeconds>45)return "Seit 45 Sekunden keine neuen Videobilder";
  if(!HadVideo&&(now-Started).TotalSeconds>45)return "Innerhalb von 45 Sekunden kein Videobild empfangen";
  if(state==2&&(now-(HadVideo?LastVideo:StateSince)).TotalSeconds>30)return "Seit 30 Sekunden ohne Fortschritt am Puffern";
  return null;
 }
}
public class VideoTile : Panel {
 Panel surface=new Panel {Dock=DockStyle.Fill,BackColor=Color.Black}; Label title=new Label {Dock=DockStyle.Top,Height=29,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0)};
 Label state=new Label {Dock=DockStyle.Bottom,Height=25,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0),ForeColor=Color.Silver};
 Label message=new Label {Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.Gray};
 static readonly SemaphoreSlim startGate=new SemaphoreSlim(2);
 System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=1000}; IntPtr player,activeMedia; Camera camera; Settings settings; bool demo,closing,retiring,starting,native,audience; DateTime since,retry; int failures; PlaybackHealth health;RawCameraSource raw;long previousInput,previousFrames;int previousDisplayed;DateTime inputRateAt;double inputRate,inputFps,displayFps;
 public bool HasCamera {get{return camera!=null;}}
 public int DisplayedFrames {get {Vlc.Stats stats;return activeMedia!=IntPtr.Zero && Vlc.libvlc_media_get_stats(activeMedia,out stats)?stats.Displayed:0;}}
 Queue<string> history=new Queue<string>();int loggedState=-1;DateTime logAt=DateTime.MinValue;
 void Log(string text){history.Enqueue(DateTime.Now.ToString("HH:mm:ss")+" "+text);while(history.Count>35)history.Dequeue();}
 public string Diagnostic(){return "SpielfeldTV 0.8 · Wiedergabediagnose\r\nKamera: "+camera.Host+":"+(native?camera.DvripPort:camera.Port)+"\r\nZugang: "+(native?"XM / DVR-IP":"RTSP")+"; Transport: "+(native||camera.Tcp?"TCP":"UDP")+"; Kamera-Bildrate: "+(raw==null?"unbekannt":raw.Fps.ToString())+"; Eingang gemessen: "+(inputRate/1024).ToString("0.0")+" KiB/s; Eingang: "+inputFps.ToString("0.0")+" FPS; Anzeige: "+displayFps.ToString("0.0")+" FPS; Mehrkamera: "+settings.Multicam+"; Hardware: "+settings.Hardware+"; Puffer: "+settings.Cache+" ms\r\nStream-Pfad und Zugangsdaten sind ausgeblendet.\r\nStatus: "+state.Text+"\r\n"+String.Join("\r\n",history.ToArray());}
 public event EventHandler FocusRequested;
 public VideoTile(Camera c,Settings s,bool isDemo,int position,bool customerView=false) {
  camera=c;settings=s;demo=isDemo;audience=customerView;Dock=DockStyle.Fill;Margin=new Padding(3);BackColor=UI.Card;
  title.Text=c==null?"Platz "+position+" · nicht belegt":c.Name;title.ForeColor=UI.Ink;
  surface.Controls.Add(message);Controls.Add(surface);Controls.Add(state);Controls.Add(title);
  if(audience){Margin=new Padding(3);BackColor=Color.Black;state.Visible=false;title.Dock=DockStyle.Bottom;title.Height=40;title.Padding=new Padding(13,0,12,0);title.Font=new Font("Segoe UI",12,FontStyle.Regular);title.BackColor=Color.FromArgb(18,23,30);title.ForeColor=Color.FromArgb(240,242,245);title.AutoEllipsis=true;title.UseMnemonic=false;message.Font=new Font("Segoe UI",11);message.ForeColor=Color.FromArgb(150,159,172);if(c==null&&!demo){title.Visible=false;message.Visible=false;}if(c==null&&demo)title.Text="Kamera "+position;}
  title.DoubleClick+=Focus;surface.DoubleClick+=Focus;message.DoubleClick+=Focus;
  timer.Tick+=(a,b)=>Tick();
  HandleCreated+=(a,b)=>{if(c!=null || demo) { retry=DateTime.UtcNow.AddMilliseconds(position*120);timer.Start();}else {message.Text=audience?"":"Kamera in der Verwaltung zuweisen";state.Text="Frei";}};
  Disposed+=(a,b)=>{closing=true;timer.Stop();timer.Dispose();Retire();};
 }
 public void AttachViewerMenu(ContextMenuStrip menu){ContextMenuStrip=surface.ContextMenuStrip=title.ContextMenuStrip=message.ContextMenuStrip=menu;}
 void Focus(object s,EventArgs e) {if(audience&&camera==null&&!demo)return;if(FocusRequested!=null)FocusRequested(this,EventArgs.Empty);}
 async void Retire() {
  if(raw!=null){raw.Dispose();raw=null;}
  var old=player;var oldMedia=activeMedia;player=activeMedia=IntPtr.Zero;if(old==IntPtr.Zero)return;retiring=true;
  try {await Task.Run(()=>{Vlc.libvlc_media_player_stop(old);Vlc.libvlc_media_player_release(old);if(oldMedia!=IntPtr.Zero)Vlc.libvlc_media_release(oldMedia);});} catch(Exception) {} finally {retiring=false;}
 }
 async void StartVideo() {
  if(closing||retiring||starting||camera==null)return;
  IntPtr media=IntPtr.Zero;starting=true;
  try {
   if(!await Vlc.InitAsync()){if(!closing){message.Text=audience?"Livebild momentan nicht verfügbar":Vlc.Failure;state.Text="VLC fehlt";timer.Stop();}return;}if(closing)return;
   native=NativeBridge.Use(camera);string url;if(native){var source=await RawCameraSource.Open(camera);if(closing){source.Dispose();return;}raw=source;previousInput=previousFrames=0;previousDisplayed=0;inputRateAt=DateTime.UtcNow;url=source.Url;}else url=camera.Url();if(closing)return;
   media=Vlc.libvlc_media_new_location(Vlc.Instance,Vlc.Utf(url));if(media==IntPtr.Zero)throw new Exception();
   foreach(var option in new[]{native||camera.Tcp?":rtsp-tcp":":no-rtsp-tcp",":network-caching="+settings.Cache,":avcodec-hw="+(settings.Hardware?"any":"none"),":no-audio",":rtsp-timeout=5"}) Vlc.libvlc_media_add_option(media,Vlc.Utf(option));
   Vlc.libvlc_media_add_option(media,Vlc.Utf(":rtsp-frame-buffer-size=4000000"));
   Vlc.libvlc_media_add_option(media,Vlc.Utf(":avcodec-threads="+(settings.Multicam?2:0)));Vlc.libvlc_media_add_option(media,Vlc.Utf(settings.Multicam?":drop-late-frames":":no-drop-late-frames"));
   if(raw!=null){Vlc.libvlc_media_add_option(media,Vlc.Utf(":demux="+raw.Demux));Vlc.libvlc_media_add_option(media,Vlc.Utf(":"+raw.Demux+"-fps="+raw.Fps));}
   IntPtr outputHandle=surface.Handle,pendingMedia=media,createdPlayer=IntPtr.Zero;
   message.Visible=true;message.Text="Livebild wird verbunden …";state.Text="Verbinde …";
   await startGate.WaitAsync();
   try{createdPlayer=await Task.Run(()=>{IntPtr pending=IntPtr.Zero;try{if(Volatile.Read(ref closing))throw new OperationCanceledException();pending=Vlc.libvlc_media_player_new_from_media(pendingMedia);if(pending==IntPtr.Zero)throw new Exception("Videoplayer konnte nicht angelegt werden.");Vlc.libvlc_media_player_set_hwnd(pending,outputHandle);Vlc.libvlc_video_set_mouse_input(pending,0);Vlc.libvlc_video_set_key_input(pending,0);Vlc.libvlc_audio_set_mute(pending,1);if(Volatile.Read(ref closing))throw new OperationCanceledException();if(Vlc.libvlc_media_player_play(pending)!=0)throw new Exception("Videoplayer konnte nicht gestartet werden.");return pending;}catch{if(pending!=IntPtr.Zero){Vlc.libvlc_media_player_stop(pending);Vlc.libvlc_media_player_release(pending);}throw;}});}finally{startGate.Release();}
   if(closing){await Task.Run(()=>{Vlc.libvlc_media_player_stop(createdPlayer);Vlc.libvlc_media_player_release(createdPlayer);});return;}
   player=createdPlayer;activeMedia=media;media=IntPtr.Zero;since=DateTime.UtcNow;health=new PlaybackHealth(since);loggedState=-1;Log("Wiedergabe starten");
  } catch(Exception ex) {if(!closing)Fail(native?ex.Message:"Video konnte nicht gestartet werden");} finally {starting=false;if(media!=IntPtr.Zero)Vlc.libvlc_media_release(media);}
 }
 void Fail() {Fail("Video konnte nicht gestartet werden");}
 void Fail(string reason) { Log(reason);failures++; Retire(); int seconds=Math.Min(30,3+failures*3);retry=DateTime.UtcNow.AddSeconds(seconds);message.Visible=true;message.Text=audience?"Livebild momentan nicht verfügbar\nVerbindung wird wiederhergestellt …":reason+"\nIn der Vorschau Diagnose kopieren, falls das Problem bestehen bleibt";state.Text="Offline · "+reason+" · neuer Versuch in "+seconds+" s";state.ForeColor=Color.SandyBrown; }
 void Tick() {
  if(closing)return;
  if(demo) {message.Text="DEMO · KEIN LIVEBILD\n"+DateTime.Now.ToString("HH:mm:ss");message.ForeColor=UI.Accent;state.Text="Demoansicht · Platzhalter";return;}
  if(player==IntPtr.Zero){if(DateTime.UtcNow>=retry && !retiring)StartVideo();return;}
  int current=Vlc.libvlc_media_player_get_state(player),frames=DisplayedFrames;var now=DateTime.UtcNow;
  if(current!=loggedState||(now-logAt).TotalSeconds>=5){Vlc.Stats stats;bool available=Vlc.libvlc_media_get_stats(activeMedia,out stats);Log("VLC="+current+" (0=Start,1=Öffnen,2=Puffern,3=Spielt,4=Pause,5=Gestoppt,6=Ende,7=Fehler)"+(available?"; Bytes="+stats.Read+"; decodiert="+stats.DecodedVideo+"; angezeigt="+stats.Displayed+"; verworfen="+stats.Lost:"; keine Videostatistik"));loggedState=current;logAt=now;}
  if(raw!=null&&(now-inputRateAt).TotalSeconds>=4){long count=raw.Received;double elapsed=(now-inputRateAt).TotalSeconds;inputRate=(count-previousInput)/elapsed;long totalFrames=raw.Frames;inputFps=(totalFrames-previousFrames)/elapsed;displayFps=Math.Max(0,frames-previousDisplayed)/elapsed;previousFrames=totalFrames;previousDisplayed=frames;previousInput=count;inputRateAt=now;}string reason=health.Observe(current,frames,now);if(reason!=null){Fail(reason);return;}
  if(health.HadVideo)message.Visible=false;
  if(current==3){double stalled=(now-health.LastVideo).TotalSeconds;state.Text=!health.HadVideo?"Warte auf Video …":stalled>3?"Bild stockt · seit "+(int)stalled+" s keine neuen Bilder":"Live · Ton aus · "+(native?"XM · Eingang "+inputFps.ToString("0.0")+" / Anzeige "+displayFps.ToString("0.0")+" FPS":camera.Tcp?"TCP":"UDP");state.ForeColor=stalled>3?Color.SandyBrown:UI.Accent;if((now-since).TotalSeconds>45&&stalled<3)failures=0;}
  else {state.Text=current==2?"Puffert … · Verbindung bleibt bestehen":"Verbinde …";state.ForeColor=Color.Silver;}
 }
}
public class CameraForm : Form {
 public Camera Result; TextBox name=new TextBox(),host=new TextBox(),user=new TextBox(),password=new TextBox {UseSystemPasswordChar=true}; ComboBox path=new ComboBox {DropDownStyle=ComboBoxStyle.DropDown},backend=UI.Combo(); NumericUpDown port=new NumericUpDown {Minimum=1,Maximum=65535},dvrip=new NumericUpDown{Minimum=1,Maximum=65535}; CheckBox tcp=new CheckBox{Text="RTSP über TCP (sonst UDP)",AutoSize=true}; Settings settings; CancellationTokenSource workCancel; TextBox diagnosis=new TextBox{Dock=DockStyle.Bottom,Height=100,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text="Automatisch nutzt für erkannte XM-Kameras den nativen DVR/IP-Zugang. Sonst RTSP."};
 public CameraForm(Camera c,Settings s) {
  settings=s;Result=c.Copy();Text="Kamera einrichten · Version 0.8";Size=new Size(960,Math.Min(900,Screen.PrimaryScreen.WorkingArea.Height-45));MinimumSize=new Size(760,620);UI.Style(this);
  var table=new TableLayoutPanel {Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(18),ColumnCount=2};table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  path.Items.AddRange(new object[]{"/onvif1","/onvif2","/1","/2","/user={user}_password={password}_channel=0_stream=1.sdp?"});
  backend.Items.AddRange(new object[]{"Automatisch","RTSP direkt","XM / VMS (DVR/IP)"});backend.SelectedIndex=c.Backend=="XM"?2:c.Backend=="RTSP"?1:0;
  name.Text=c.Name;host.Text=c.Host;user.Text=c.User;password.Text=c.Password;path.Text=c.Path;port.Value=c.Port;tcp.Checked=c.Tcp;dvrip.Value=c.DvripPort;
  Control[] fields={name,host,backend,port,dvrip,user,password,path,tcp};string[] labels={"Name / Spielfeld","IP-Adresse / Hostname","Kamera-Zugang","RTSP-Port","XM / DVR-IP-Port","Gerätebenutzer","Gerätepasswort","RTSP-Pfad","RTSP-Transport"};
  for(int i=0;i<fields.Length;i++){fields[i].Dock=DockStyle.Fill;fields[i].Margin=new Padding(5,8,5,8);table.Controls.Add(UI.Label(labels[i]),0,i);table.Controls.Add(fields[i],1,i);}
  var tip=UI.Label("Für XM/iCSee-Kameras wie bei VMS: Automatisch oder XM / DVR-IP wählen.\nStream automatisch ermitteln liest die Kameraprofile. Kleinen Videostream wählen.\nEin Videoprofil ist noch kein bestätigtes Livebild: danach Vorschau testen.");tip.Dock=DockStyle.Top;tip.Padding=new Padding(12);tip.MaximumSize=new Size(900,0);
  var bar=UI.Bar();bar.Dock=DockStyle.Bottom;
  bar.Controls.Add(UI.Button("Stream automatisch ermitteln",async(a,b)=>{if(workCancel!=null)return;try{var cam=Read();workCancel=new CancellationTokenSource();table.Enabled=bar.Enabled=false;var profiles=await Onvif.Read(cam,workCancel.Token,new Progress<string>(text=>{if(!IsDisposed)diagnosis.Text=text;}));if(IsDisposed)return;using(var select=new ProfileForm(profiles)){if(select.ShowDialog(this)==DialogResult.OK){path.Text=select.Selected.Path;port.Value=select.Selected.Port;Result.Service=select.Selected.Service;diagnosis.Text="Kameraprofil übernommen. Jetzt Livebild testen und anschließend speichern.";}}}catch(OperationCanceledException){}catch(Exception ex){if(!IsDisposed)diagnosis.Text=ex.Message;}finally{if(!IsDisposed)table.Enabled=bar.Enabled=true;if(workCancel!=null){workCancel.Dispose();workCancel=null;}}}));
  bar.Controls.Add(UI.Button("Verbindung prüfen",async(a,b)=>{if(workCancel!=null)return;try{var cam=Read();workCancel=new CancellationTokenSource();table.Enabled=bar.Enabled=false;diagnosis.Text="RTSP und Anmeldung werden geprüft …";var result=await Rtsp.Check(cam,workCancel.Token);if(!IsDisposed)diagnosis.Text=result.Summary;}catch(OperationCanceledException){}catch(Exception ex){if(!IsDisposed)diagnosis.Text=ex.Message;}finally{if(!IsDisposed)table.Enabled=bar.Enabled=true;if(workCancel!=null){workCancel.Dispose();workCancel=null;}}}));
  bar.Controls.Add(UI.Button("Livebild testen",(a,b)=>{try {var cam=Read();if(!Vlc.Init()){MessageBox.Show(Vlc.Failure);return;}using(var preview=new PreviewForm(cam,settings))preview.ShowDialog(this);tcp.Checked=cam.Tcp;}catch(Exception ex){UI.Error(ex);}}));
  bar.Controls.Add(UI.Button("Speichern",(a,b)=>{try{Result=Read();DialogResult=DialogResult.OK;Close();}catch(Exception ex){UI.Error(ex);}}));bar.Controls.Add(UI.Button("Abbrechen",(a,b)=>Close()));
  var body=new Panel{Dock=DockStyle.Fill,AutoScroll=true};body.Controls.Add(tip);body.Controls.Add(table);Controls.Add(body);Controls.Add(diagnosis);Controls.Add(bar);FormClosing+=(a,b)=>{if(workCancel!=null)workCancel.Cancel();};
 }
 Camera Read() {var c=Result.Copy();c.Name=name.Text.Trim();c.Host=host.Text.Trim();c.Port=(int)port.Value;c.User=user.Text;c.Password=password.Text;c.Path=path.Text.Trim();c.Tcp=tcp.Checked;c.DvripPort=(int)dvrip.Value;c.Backend=backend.SelectedIndex==2?"XM":backend.SelectedIndex==1?"RTSP":"Auto";c.Validate();return c;}
}
public class PreviewForm : Form {
 Panel area=new Panel{Dock=DockStyle.Fill};Camera camera;Settings options;CheckBox software=new CheckBox{Text="Ohne Hardwaredecodierung testen",AutoSize=true,Margin=new Padding(8,12,8,8)},tcp=new CheckBox{Text="TCP",AutoSize=true,Margin=new Padding(8,12,8,8)};
 public PreviewForm(Camera c,Settings s) {camera=c;options=new Settings{Cache=s.Cache,Hardware=s.Hardware,Multicam=s.Multicam};Text="Verbindungsprüfung · "+c.Name;Size=new Size(1040,660);UI.Style(this);var bar=UI.Bar();software.Checked=!s.Hardware;tcp.Checked=c.Tcp;bar.Controls.Add(software);bar.Controls.Add(tcp);bar.Controls.Add(UI.Button("Diagnose kopieren",(a,b)=>{var tile=area.Controls.OfType<VideoTile>().FirstOrDefault();if(tile!=null){try{Clipboard.SetText(tile.Diagnostic());MessageBox.Show("Diagnose kopiert. Hier im Chat einfügen. Stream-Adresse und Zugangsdaten sind ausgeblendet.","Wiedergabediagnose");}catch(Exception ex){UI.Error(ex);}}}));bar.Controls.Add(UI.Label("Bei schwarzem Bild Modus wechseln. Dauerhaft: Start & Wiedergabe."));software.CheckedChanged+=(a,b)=>Restart();tcp.CheckedChanged+=(a,b)=>Restart();Controls.Add(area);Controls.Add(bar);Restart();}
 void Restart(){foreach(Control child in area.Controls.Cast<Control>().ToArray())child.Dispose();options.Hardware=!software.Checked;camera.Tcp=tcp.Checked;area.Controls.Add(new VideoTile(camera,options,false,1));}
}
public class DisplayForm : Form {
 Settings settings;bool demo;TableLayoutPanel table;List<VideoTile> tiles=new List<VideoTile>();VideoTile focused;Dictionary<VideoTile,TableLayoutPanelCellPosition> positions=new Dictionary<VideoTile,TableLayoutPanelCellPosition>();List<ViewPage> pages;string screenName;int pageIndex;Panel canvas=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(8,11,15),Padding=new Padding(5)};ContextMenuStrip menu=new ContextMenuStrip();
 public string Diagnostics(){return String.Join("\r\n\r\n",tiles.Where(t=>t.HasCamera).Select(t=>t.Diagnostic()));}
 public DisplayForm(Settings s,Screen screen,List<ViewPage> views,bool isDemo){settings=s;demo=isDemo;pages=views;screenName=screen.DeviceName;Text="SpielfeldTV · "+screen.DeviceName;UI.Style(this);BackColor=Color.FromArgb(8,11,15);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=screen.Bounds;KeyPreview=true;Controls.Add(canvas);var heading=new Label{Text=ViewerTitles.Get(s,screenName),Dock=DockStyle.Top,Height=58,Padding=new Padding(18,0,18,0),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,UseMnemonic=false,ForeColor=Color.FromArgb(240,242,245),BackColor=Color.FromArgb(18,23,30),Font=new Font("Segoe UI",18,FontStyle.Regular),AccessibleName="Spielfeld-Überschrift"};Controls.Add(heading);ContextMenuStrip=menu;canvas.ContextMenuStrip=menu;menu.Opening+=(a,b)=>BuildMenu();int saved=pages.FindIndex(p=>settings.ActivePages.Contains(screenName+"|"+p.Name));pageIndex=Math.Max(0,saved);Shown+=(a,b)=>LoadPage();FormClosed+=(a,b)=>{ClearTiles();menu.Dispose();};KeyDown+=(a,b)=>{if(b.KeyCode==Keys.Escape){if(focused!=null)FocusTile(focused);else Close();b.Handled=true;}else if(b.KeyCode==Keys.F11){ToggleFull();b.Handled=true;}else if(b.KeyCode==Keys.PageDown||(b.Control&&b.KeyCode==Keys.Tab)){SelectView((pageIndex+(b.Shift?-1:1)+pages.Count)%pages.Count);b.SuppressKeyPress=true;}else if(b.KeyCode==Keys.PageUp){SelectView((pageIndex-1+pages.Count)%pages.Count);b.SuppressKeyPress=true;}};}
 void BuildMenu(){foreach(ToolStripItem item in menu.Items.Cast<ToolStripItem>().ToArray())item.Dispose();menu.Items.Clear();if(pages.Count>1){var views=new ToolStripMenuItem("Ansicht wechseln");for(int i=0;i<pages.Count;i++){int index=i;var item=new ToolStripMenuItem(pages[i].Name){Checked=i==pageIndex};item.Click+=(a,b)=>SelectView(index);views.DropDownItems.Add(item);}menu.Items.Add(views);menu.Items.Add(new ToolStripSeparator());}var raster=new ToolStripMenuItem("Zur Rasteransicht"){Enabled=focused!=null};raster.Click+=(a,b)=>{if(focused!=null)FocusTile(focused);};menu.Items.Add(raster);menu.Items.Add("Vollbild / Fenster  ·  F11",null,(a,b)=>ToggleFull());menu.Items.Add("Zuschaueranzeige schließen",null,(a,b)=>Close());}
 public void SelectView(int index){if(index<0||index>=pages.Count)return;pageIndex=index;LoadPage();}
 void ToggleFull(){if(FormBorderStyle==FormBorderStyle.None){FormBorderStyle=FormBorderStyle.Sizable;Bounds=Screen.FromControl(this).WorkingArea;}else{FormBorderStyle=FormBorderStyle.None;Bounds=Screen.FromControl(this).Bounds;}}
 void ClearTiles(){foreach(var t in tiles)t.Dispose();tiles.Clear();positions.Clear();focused=null;if(table!=null){table.Dispose();table=null;}}
 void LoadPage(){if(pages.Count==0)return;ClearTiles();var p=pages[pageIndex];settings.ActivePages.RemoveAll(x=>x.StartsWith(screenName+"|",StringComparison.Ordinal));settings.ActivePages.Add(screenName+"|"+p.Name);table=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=p.Columns,RowCount=p.Rows,BackColor=canvas.BackColor,Margin=Padding.Empty,Padding=Padding.Empty,ContextMenuStrip=menu};for(int c=0;c<p.Columns;c++)table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/p.Columns));for(int r=0;r<p.Rows;r++)table.RowStyles.Add(new RowStyle(SizeType.Percent,100f/p.Rows));canvas.Controls.Add(table);table.SuspendLayout();for(int i=0;i<p.Rows*p.Columns;i++){var cam=settings.Cameras.FirstOrDefault(x=>x.Id==p.Slots[i]);var tile=new VideoTile(cam,settings,demo,i+1,true);tile.AttachViewerMenu(menu);tile.FocusRequested+=(a,b)=>FocusTile((VideoTile)a);table.Controls.Add(tile,i%p.Columns,i/p.Columns);tiles.Add(tile);positions[tile]=new TableLayoutPanelCellPosition(i%p.Columns,i/p.Columns);}table.ResumeLayout();}
 void FocusTile(VideoTile t){table.SuspendLayout();if(focused!=null){table.SetColumnSpan(focused,1);table.SetRowSpan(focused,1);table.SetCellPosition(focused,positions[focused]);foreach(var tile in tiles)tile.Visible=true;if(focused==t){focused=null;table.ResumeLayout();return;}}focused=t;foreach(var tile in tiles)tile.Visible=tile==t;table.SetCellPosition(t,new TableLayoutPanelCellPosition(0,0));table.SetColumnSpan(t,table.ColumnCount);table.SetRowSpan(t,table.RowCount);table.ResumeLayout();}
}
public class Choice {public string Id,Name;public override string ToString(){return Name;}}
public partial class MainForm : Form {
 Settings settings; DataGridView cameras=UI.Grid(); ListBox pages=new ListBox {Dock=DockStyle.Fill,BackColor=UI.Card,ForeColor=UI.Ink,BorderStyle=BorderStyle.None,ItemHeight=30}; ComboBox monitor=UI.Combo(); NumericUpDown rows=new NumericUpDown {Minimum=1,Maximum=5,Width=65},cols=new NumericUpDown {Minimum=1,Maximum=5,Width=65},cache=new NumericUpDown {Minimum=100,Maximum=3000,Increment=100,Width=90}; TextBox pageName=new TextBox {Width=180};TableLayoutPanel slots=new TableLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(8)};CheckBox auto=new CheckBox {Text="Gespeicherte Zuschauerfenster beim Programmstart öffnen",AutoSize=true},hw=new CheckBox {Text="Hardwaredecodierung verwenden",AutoSize=true};Label info=UI.Label("");List<DisplayForm> displays=new List<DisplayForm>();bool loading,launching;
 public MainForm(Settings s) {
  settings=s;DisplayConfigNames.Resolve(settings);ViewerTitles.Ensure(settings);Text="SpielfeldTV 0.8 · Verwaltung";Size=new Size(1240,830);MinimumSize=new Size(1050,690);UI.Style(this);
  var header=UI.Bar();header.Padding=new Padding(18);header.Controls.Add(new Label {Text="SPIELFELD TV",Font=new Font("Segoe UI",20,FontStyle.Bold),ForeColor=UI.Accent,AutoSize=true,Margin=new Padding(0,0,25,0)});header.Controls.Add(UI.Button("Zuschaueranzeigen starten",(a,b)=>Launch(false)));header.Controls.Add(UI.Button("Demo ansehen",(a,b)=>Launch(true)));header.Controls.Add(UI.Button("Anzeigen schließen",(a,b)=>CloseDisplays()));header.Controls.Add(UI.Button("Alles speichern",(a,b)=>Save()));
  var tab=new TabControl {Dock=DockStyle.Fill};var ct=new TabPage("1  Kameras hinzufügen");var lt=new TabPage("2  Raster & Tabs");var st=new TabPage("3  Start & Wiedergabe");foreach(var t in new[]{ct,lt,st}){t.BackColor=UI.Bg;t.ForeColor=UI.Ink;tab.TabPages.Add(t);}
  cameras.Columns.Add("name","Kamera / Spielfeld");cameras.Columns.Add("ip","IP-Adresse");cameras.Columns.Add("port","Port");cameras.Columns.Add("path","Stream-Pfad");cameras.CellDoubleClick+=(a,b)=>EditCamera();
  var cb=UI.Bar();cb.Controls.Add(UI.Button("Netzwerk durchsuchen",(a,b)=>Discover()));cb.Controls.Add(UI.Button("Manuell hinzufügen",(a,b)=>EditCamera(new Camera())));cb.Controls.Add(UI.Button("Bearbeiten / Livebild testen",(a,b)=>EditCamera()));cb.Controls.Add(UI.Button("Entfernen",(a,b)=>DeleteCamera()));ct.Controls.Add(cameras);ct.Controls.Add(cb);
  var left=new Panel {Dock=DockStyle.Left,Width=210,Padding=new Padding(8)};var pb=UI.Bar();pb.Dock=DockStyle.Bottom;pb.Controls.Add(UI.Button("+ Tab",(a,b)=>{CommitPage();var p=new ViewPage {Name="Ansicht "+(settings.Pages.Count+1),Screen=Screen.AllScreens[0].DeviceName};p.Normalize();settings.Pages.Add(p);RefreshPages(p);}));pb.Controls.Add(UI.Button("Löschen",(a,b)=>DeletePage()));left.Controls.Add(pages);left.Controls.Add(pb);
  var right=new Panel {Dock=DockStyle.Fill};var layoutbar=UI.Bar();layoutbar.Controls.Add(UI.Label("Tab-Name"));layoutbar.Controls.Add(pageName);layoutbar.Controls.Add(UI.Label("Bildschirm"));layoutbar.Controls.Add(monitor);layoutbar.Controls.Add(UI.Label("Zeilen"));layoutbar.Controls.Add(rows);layoutbar.Controls.Add(UI.Label("Spalten"));layoutbar.Controls.Add(cols);
  var instruction=UI.Label("Jeder Platz entspricht der Position auf dem Fernseher. Kamera über das Auswahlfeld zuweisen.\nMehrere Tabs auf demselben Bildschirm sind möglich; jeweils nur der gewählte Tab spielt ab.");instruction.Dock=DockStyle.Top;instruction.Padding=new Padding(10);
  right.Controls.Add(slots);right.Controls.Add(instruction);right.Controls.Add(layoutbar);lt.Controls.Add(right);lt.Controls.Add(left);
  monitor.DropDownWidth=400;foreach(var screen in Screen.AllScreens)monitor.Items.Add(new Choice {Id=screen.DeviceName,Name=MonitorNames.Label(screen)});
  pages.SelectedIndexChanged+=(a,b)=>LoadPageEditor();pageName.TextChanged+=(a,b)=>{if(!loading){CommitPage();pages.Invalidate();}};monitor.SelectedIndexChanged+=(a,b)=>{if(!loading)CommitPage();};rows.ValueChanged+=(a,b)=>ResizeGrid();cols.ValueChanged+=(a,b)=>ResizeGrid();
  pages.DrawMode=DrawMode.OwnerDrawFixed;pages.DrawItem+=(a,b)=>{if(b.Index<0)return;b.DrawBackground();TextRenderer.DrawText(b.Graphics,((ViewPage)pages.Items[b.Index]).Name,b.Font,b.Bounds,UI.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);b.DrawFocusRectangle();};
  var options=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(22),WrapContents=false,AutoScroll=true};auto.Checked=settings.AutoOpen;hw.Checked=settings.Hardware;cache.Value=Math.Max(100,Math.Min(3000,settings.Cache));options.Controls.Add(auto);options.Controls.Add(hw);options.Controls.Add(UI.Label("Videopuffer in Millisekunden (kleiner = weniger Verzögerung, größer = mehr Reserve):"));options.Controls.Add(cache);options.Controls.Add(UI.Label("Für die Rasteransicht möglichst den kleineren Kamerastream verwenden.\nAlle Bilder laufen ohne Ton. Die tatsächliche Leistung hängt von PC, Codec und Netzwerk ab.\nDie Software zeichnet nicht auf.\n\nWindows-Anzeigeeinstellung: Diese Anzeigen erweitern.\nF11: Vollbild/Fenster. Esc: Einzelbild verlassen oder Zuschauerfenster schließen.\nDoppelklick auf Kameranamen oder Bild: Einzelbild / Raster.\nZuschaueransicht: nur Kameras und Namen. Rechtsklick: Ansichten und Bedienung.\nBild auf / Bild ab oder Strg+Tab: zwischen Ansichten wechseln.\n\nAutomatisches Öffnen gilt beim Start dieser Anwendung. Windows-Start lässt sich unten aktivieren.\nNach Änderungen: speichern und Zuschaueranzeigen neu starten.\nMaximal drei verschiedene Bildschirme gleichzeitig; Raster bis 5 × 5 pro Tab."));options.Controls.Add(UI.Button("VLC-Download öffnen",(a,b)=>System.Diagnostics.Process.Start("https://www.videolan.org/vlc/download-windows.html")));st.Controls.Add(options);
  info.Dock=DockStyle.Bottom;info.Padding=new Padding(12);info.Height=48;info.AutoSize=false;Controls.Add(tab);Controls.Add(info);Controls.Add(header);
  AddFeatures(tab);RefreshCameras();RefreshPages(settings.Pages.FirstOrDefault());
  Shown+=(a,b)=>{if(settings.AutoOpen)Launch(false);};FormClosing+=(a,b)=>{if(!Save()){b.Cancel=true;return;}CloseDisplays();};
 }
 void RefreshCameras(){cameras.Rows.Clear();foreach(var c in settings.Cameras){int i=cameras.Rows.Add(c.Name,c.Host,c.Port,c.Path);cameras.Rows[i].Tag=c;}info.Text=settings.Cameras.Count+" Kameras eingerichtet · "+Screen.AllScreens.Length+" Bildschirm(e) erkannt · lokale Liveanzeige";}
 Camera CurrentCamera(){return cameras.CurrentRow==null?null:cameras.CurrentRow.Tag as Camera;}
 void EditCamera(){var c=CurrentCamera();if(c!=null)EditCamera(c);}
 void EditCamera(Camera c){using(var f=new CameraForm(c,settings)){if(f.ShowDialog(this)!=DialogResult.OK)return;int index=settings.Cameras.FindIndex(x=>x.Id==c.Id);if(index<0)settings.Cameras.Add(f.Result);else settings.Cameras[index]=f.Result;RefreshCameras();LoadPageEditor();Save();}}
 void Discover(){using(var f=new DiscoveryForm()){if(f.ShowDialog(this)!=DialogResult.OK)return;int added=0;foreach(var d in f.Selected){var existing=settings.Cameras.FirstOrDefault(x=>x.Host==d.Host && x.Port==d.Port);if(existing!=null){if(d.Service.Length>0)existing.Service=d.Service;continue;}settings.Cameras.Add(new Camera {Name=d.Name,Host=d.Host,Port=d.Port,Service=d.Service});added++;}RefreshCameras();LoadPageEditor();Save();MessageBox.Show(added+" Einträge hinzugefügt. Jetzt Bearbeiten wählen, Geräte-Zugangsdaten eingeben und Stream automatisch ermitteln.","Kameras hinzufügen");}}
 void DeleteCamera(){var c=CurrentCamera();if(c==null)return;if(MessageBox.Show("Kamera '"+c.Name+"' entfernen?","Kamera entfernen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;settings.Cameras.Remove(c);foreach(var p in settings.Pages)for(int i=0;i<p.Slots.Count;i++)if(p.Slots[i]==c.Id)p.Slots[i]="";RefreshCameras();LoadPageEditor();Save();}
 ViewPage CurrentPage(){return pages.SelectedItem as ViewPage;}
 void RefreshPages(ViewPage select){loading=true;pages.Items.Clear();foreach(var p in settings.Pages)pages.Items.Add(p);if(select!=null)pages.SelectedItem=select;loading=false;LoadPageEditor();}
 void DeletePage(){var p=CurrentPage();if(p==null)return;if(settings.Pages.Count==1){MessageBox.Show("Mindestens einen Tab behalten.");return;}if(MessageBox.Show("Tab entfernen?","Raster",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;settings.Pages.Remove(p);RefreshPages(settings.Pages[0]);Save();}
 void CommitPage(){if(loading)return;var p=CurrentPage();if(p==null)return;p.Name=pageName.Text;var m=monitor.SelectedItem as Choice;if(m!=null){p.Screen=m.Id;p.ScreenKey=DisplayConfigNames.Key(m.Id);}p.Rows=(int)rows.Value;p.Columns=(int)cols.Value;p.Normalize();}
 void LoadPageEditor(){if(loading)return;var p=CurrentPage();if(p==null)return;loading=true;pageName.Text=p.Name;rows.Value=p.Rows;cols.Value=p.Columns;monitor.SelectedIndex=-1;foreach(Choice item in monitor.Items)if(item.Id==p.Screen)monitor.SelectedItem=item;loading=false;BuildSlots();}
 void ResizeGrid(){if(loading)return;CommitPage();BuildSlots();}
 void BuildSlots(){var p=CurrentPage();if(p==null)return;slots.SuspendLayout();foreach(Control child in slots.Controls.Cast<Control>().ToArray())child.Dispose();slots.Controls.Clear();slots.ColumnStyles.Clear();slots.RowStyles.Clear();slots.ColumnCount=p.Columns;slots.RowCount=p.Rows;for(int c=0;c<p.Columns;c++)slots.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/p.Columns));for(int r=0;r<p.Rows;r++)slots.RowStyles.Add(new RowStyle(SizeType.Percent,100f/p.Rows));
  for(int i=0;i<p.Rows*p.Columns;i++){int index=i;var cell=new Panel {Dock=DockStyle.Fill,BackColor=UI.Card,Margin=new Padding(4),MinimumSize=new Size(100,70)};var label=UI.Label("Platz "+(i+1)+" · "+(i/p.Columns+1)+"/"+(i%p.Columns+1));label.Dock=DockStyle.Top;var combo=UI.Combo();combo.Dock=DockStyle.Bottom;combo.Items.Add(new Choice {Id="",Name="— frei —"});foreach(var cam in settings.Cameras)combo.Items.Add(new Choice{Id=cam.Id,Name=cam.Name});combo.SelectedIndex=0;foreach(Choice item in combo.Items)if(item.Id==p.Slots[i])combo.SelectedItem=item;combo.SelectedIndexChanged+=(a,b)=>p.Slots[index]=((Choice)combo.SelectedItem).Id;cell.Controls.Add(combo);cell.Controls.Add(label);slots.Controls.Add(cell,i%p.Columns,i/p.Columns);}
  slots.ResumeLayout();
 }
 bool Save(){try{CommitPage();settings.AutoOpen=auto.Checked;settings.Hardware=hw.Checked;settings.Cache=(int)cache.Value;Store.Save(settings);info.Text="Gespeichert · "+settings.Cameras.Count+" Kameras · "+DateTime.Now.ToString("HH:mm:ss");return true;}catch(Exception ex){UI.Error(ex);return false;}}
 void CloseDisplays(){foreach(var d in displays.ToArray())if(!d.IsDisposed)d.Close();displays.Clear();}
 async void Launch(bool demo){if(launching||!Save())return;launching=true;UseWaitCursor=true;info.Text="Zuschaueranzeigen werden vorbereitet …";try{if(!demo){bool available=await Vlc.InitAsync();if(IsDisposed||Disposing)return;if(!available){MessageBox.Show(Vlc.Failure,"VLC erforderlich");return;}}if(IsDisposed||Disposing)return;
  var screens=Screen.AllScreens;var missing=settings.Pages.Where(p=>!settings.DisabledScreens.Contains(p.Screen)&&!screens.Any(sc=>sc.DeviceName==p.Screen)).ToList();
  if(missing.Count>0){MessageBox.Show("Bildschirm für diese Tabs bitte neu auswählen: "+String.Join(", ",missing.Select(p=>p.Name)),"Bildschirm fehlt");return;}
  var groups=settings.Pages.Where(p=>!settings.DisabledScreens.Contains(p.Screen)).GroupBy(p=>p.Screen).ToList();if(groups.Count>3){MessageBox.Show("Bitte höchstens drei Bildschirme zuweisen.");return;}
  if(settings.Pages.Any(p=>String.IsNullOrWhiteSpace(p.Name))){MessageBox.Show("Bitte jedem Tab einen Namen geben.");return;}
  if(settings.Pages.GroupBy(p=>p.Screen+"|"+p.Name).Any(g=>g.Count()>1)){MessageBox.Show("Tabs am selben Bildschirm benötigen unterschiedliche Namen.");return;}
  if(!demo && !settings.Pages.Any(p=>p.Slots.Take(p.Rows*p.Columns).Any(id=>settings.Cameras.Any(c=>c.Id==id)))){MessageBox.Show("Zuerst Kameras hinzufügen und im Raster zuweisen. Ohne Kameras kannst du die Demo öffnen.");return;}
  CloseDisplays();foreach(var group in groups){var screen=screens.First(sc=>sc.DeviceName==group.Key);var f=new DisplayForm(settings,screen,group.ToList(),demo);displays.Add(f);f.Show();await Task.Yield();if(IsDisposed||Disposing)return;}Store.Save(settings);info.Text="Zuschaueranzeigen geöffnet · Kameras verbinden sich im Hintergrund.";}catch(Exception ex){if(!IsDisposed)UI.Error(ex);}finally{launching=false;if(!IsDisposed)UseWaitCursor=false;}
 }
}
public static class Program {
 [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 [STAThread] public static int Main(string[] args) {
  SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  AppDomain.CurrentDomain.ProcessExit+=(s,e)=>NativeBridge.Stop();Application.ApplicationExit+=(s,e)=>NativeBridge.Stop();
  if(args.Contains("--self-test"))return Tests.Run(args);
  if(args.Contains("--native-test"))return Tests.Native(args);
  try {Application.Run(new MainForm(Store.Load()));return 0;}catch(Exception){MessageBox.Show("Die Einstellungen konnten nicht geladen werden. Die Datei bleibt erhalten.\nBitte settings.dat unter %LOCALAPPDATA%\\SpielfeldTV sichern und umbenennen und erneut starten.\nBei wiederholten Fehlern den Entwickler kontaktieren.","SpielfeldTV");return 1;}
 }
}
public static class Tests {
 class SilentProgress : IProgress<string>{public void Report(string value){}}
 static async Task<string> ReadRequest(NetworkStream stream){var bytes=new List<byte>();var one=new byte[1];while(bytes.Count<32768){if(await stream.ReadAsync(one,0,1)==0)throw new Exception("Testverbindung unerwartet beendet");bytes.Add(one[0]);int n=bytes.Count;if(n>=4&&bytes[n-4]==13&&bytes[n-3]==10&&bytes[n-2]==13&&bytes[n-1]==10)break;}string headers=Encoding.UTF8.GetString(bytes.ToArray());var length=Regex.Match(headers,@"(?im)^Content-Length:\s*(\d+)");int size=length.Success?Int32.Parse(length.Groups[1].Value):0;var body=new byte[size];int offset=0;while(offset<size){int n=await stream.ReadAsync(body,offset,size-offset);if(n==0)throw new Exception("Testbody unvollständig");offset+=n;}return headers+Encoding.UTF8.GetString(body);}
 static async Task Reply(NetworkStream stream,string protocol,int code,string body,string extra){var data=Encoding.UTF8.GetBytes(body);var header=Encoding.ASCII.GetBytes(protocol+" "+code+" Test\r\nCSeq: 1\r\nContent-Length: "+data.Length+"\r\n"+extra+"\r\n");await stream.WriteAsync(header,0,header.Length);await stream.WriteAsync(data,0,data.Length);}
 static async Task Protocols(){
  var http=new TcpListener(IPAddress.Loopback,0);http.Start();int port=((IPEndPoint)http.LocalEndpoint).Port;
  try{var serve=Task.Run(async()=>{for(int i=0;i<3;i++)using(var client=await http.AcceptTcpClientAsync())using(var stream=client.GetStream()){
   string request=await ReadRequest(stream);Assert(request.Contains("PasswordDigest")&&!request.Contains("test-secret"),"ONVIF WS-Security-Passwortschutz");
   string body=i==0?"<GetCapabilitiesResponse><Capabilities><Media><XAddr>http://127.0.0.1:"+port+"/media</XAddr></Media></Capabilities></GetCapabilitiesResponse>":i==1?"<GetProfilesResponse><Profiles token=\"profile1\"><Name>Kleiner Stream</Name><VideoEncoderConfiguration><Encoding>H264</Encoding><Resolution><Width>640</Width><Height>360</Height></Resolution></VideoEncoderConfiguration></Profiles></GetProfilesResponse>":"<GetStreamUriResponse><MediaUri><Uri>rtsp://127.0.0.1:18554/actual-stream</Uri></MediaUri></GetStreamUriResponse>";
   await Reply(stream,"HTTP/1.1",200,"<Envelope><Body>"+body+"</Body></Envelope>","Content-Type: application/soap+xml\r\nConnection: close\r\n");
  }});var profiles=await Onvif.Read(new Camera{Host="127.0.0.1",User="tester",Password="test-secret",Service="http://127.0.0.1:"+port+"/onvif/device_service"},CancellationToken.None,new SilentProgress());await serve;Assert(profiles.Count==1&&profiles[0].Path=="/actual-stream"&&profiles[0].Port==18554&&profiles[0].Name.Contains("640×360"),"Echte ONVIF-Stream-Adresse übernehmen");}finally{http.Stop();}
  foreach(int status in new[]{200,404,401}){var rtsp=new TcpListener(IPAddress.Loopback,0);rtsp.Start();int rp=((IPEndPoint)rtsp.LocalEndpoint).Port;try{var serve=Task.Run(async()=>{using(var client=await rtsp.AcceptTcpClientAsync())using(var stream=client.GetStream()){string first=await ReadRequest(stream);Assert(first.StartsWith("DESCRIBE rtsp://127.0.0.1:"+rp+"/test "),"RTSP-Request ohne eingebettete Zugangsdaten");if(status==401){await Reply(stream,"RTSP/1.0",401,"","WWW-Authenticate: Digest realm=\"camera\", nonce=\"nonce123\", qop=\"auth\"\r\n");string second=await ReadRequest(stream);Assert(second.Contains("Authorization: Digest username=\"tester\"")&&!second.Contains("test-secret"),"Digest-Anmeldung");await Reply(stream,"RTSP/1.0",403,"","");}else await Reply(stream,"RTSP/1.0",status,status==200?"v=0\r\nm=video 0 RTP/AVP 96\r\n":"","");}});var result=await Rtsp.Check(new Camera{Host="127.0.0.1",Port=rp,Path="/test",User="tester",Password="test-secret"},CancellationToken.None);await serve;Assert(result.Code==(status==401?403:status),"RTSP-Statusdiagnose");Assert(result.Video==(status==200),"Videostream-Erkennung");}finally{rtsp.Stop();}}
  Console.WriteLine("PASS: ONVIF profile/URI exchange and WS-Security, RTSP video/404/auth diagnostics.");
 }
 static void Assert(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static IEnumerable<Control> Descendants(Control parent){foreach(Control c in parent.Controls){yield return c;foreach(var nested in Descendants(c))yield return nested;}}
 static void Snapshot(Form f,string file){using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,f.Size));bitmap.Save(file);}}
 static void Pump(int ms){var end=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(15);}}
 public static int Native(string[] args){IntPtr media=IntPtr.Zero,server=IntPtr.Zero;Form form=null;try{
  Vlc.Diagnostics=true;Assert(Vlc.Init(),Vlc.Failure);Console.WriteLine("VLC initialized.");string file=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"testbild.bmp");
  using(var b=new Bitmap(640,360))using(var g=Graphics.FromImage(b)){g.Clear(UI.Bg);g.FillRectangle(Brushes.SeaGreen,30,30,580,300);g.DrawString("LOKALER RTSP-TEST",new Font("Segoe UI",26),Brushes.White,55,140);b.Save(file,System.Drawing.Imaging.ImageFormat.Bmp);}
  media=Vlc.libvlc_media_new_location(Vlc.Instance,Vlc.Utf(new Uri(file).AbsoluteUri));
  Vlc.libvlc_media_add_option(media,Vlc.Utf(":image-duration=120"));Vlc.libvlc_media_add_option(media,Vlc.Utf(":image-fps=15"));
  Vlc.libvlc_media_add_option(media,Vlc.Utf(":sout=#transcode{vcodec=h264,venc=x264{keyint=15,tune=zerolatency,bframes=0},vb=300,fps=15}:rtp{sdp=rtsp://127.0.0.1:18554/test}"));
  server=Vlc.libvlc_media_player_new_from_media(media);Assert(Vlc.libvlc_media_player_play(server)==0,"RTSP-Testserver starten");Pump(1800);Console.WriteLine("Server state: "+Vlc.libvlc_media_player_get_state(server));
  form=new Form {Text="Lokaler RTSP-Test",Size=new Size(800,530)};UI.Style(form);var tile=new VideoTile(new Camera{Name="Lokales Testbild – keine Kamera",Host="127.0.0.1",Port=18554,Path="/test"},new Settings {Hardware=false,Cache=300},false,1);form.Controls.Add(tile);form.Show();Pump(8500);
  if(args.Length>1)Snapshot(form,args[1]);Assert(tile.DisplayedFrames>20,"RTSP-Wiedergabe: keine decodierten Bilder ("+tile.DisplayedFrames+")");Console.WriteLine("PASS: local RTSP H.264 playback, "+tile.DisplayedFrames+" displayed frames.");
  Vlc.libvlc_media_player_stop(server);Pump(48000);Assert(tile.DisplayedFrames==0,"Offline-Wiedergabe soll beendet sein");
  Assert(Vlc.libvlc_media_player_play(server)==0,"RTSP-Testserver neu starten");var reconnectDeadline=DateTime.UtcNow.AddSeconds(30);while(tile.DisplayedFrames<=10&&DateTime.UtcNow<reconnectDeadline)Pump(500);if(tile.DisplayedFrames<=10)Console.WriteLine(tile.Diagnostic());Assert(tile.DisplayedFrames>10,"Automatischer Wiederverbindungsversuch");Console.WriteLine("PASS: reconnect after local RTSP server interruption.");
  return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{if(form!=null)form.Dispose();Pump(600);if(server!=IntPtr.Zero){Vlc.libvlc_media_player_stop(server);Vlc.libvlc_media_player_release(server);}if(media!=IntPtr.Zero)Vlc.libvlc_media_release(media);}}
 public static int Run(string[] args){try {
  Assert(RawCameraSource.FrameRate(84)==20&&RawCameraSource.FrameRate(20)==20&&RawCameraSource.FrameRate(12)==12,"Kamera-Bildrate mit Markierungsbits korrekt auswerten");
  var clock=DateTime.UtcNow;var watch=new PlaybackHealth(clock);Assert(watch.Observe(3,100,clock.AddSeconds(25))==null,"Erstes Videobild");Assert(watch.Observe(2,100,clock.AddSeconds(26))==null,"Pufferpause nach langer Wiedergabe darf nicht sofort abbrechen");Assert(watch.Observe(3,120,clock.AddSeconds(29))==null,"Wiedergabe nach Pufferpause");Assert(watch.Observe(3,120,clock.AddSeconds(35))==null,"Kurzer Stillstand toleriert");Assert(watch.Observe(3,120,clock.AddSeconds(75))!=null,"Dauerhafter Bildstillstand wird erkannt");var buffering=new PlaybackHealth(clock);buffering.Observe(2,0,clock);Assert(buffering.Observe(2,0,clock.AddSeconds(31))!=null,"Dauerhaftes Puffern wird erkannt");Console.WriteLine("PASS: buffering recovery and stalled-video watchdog regression.");
  PacketTests.Run();FeatureTests.Run();Task.Run(()=>Protocols()).GetAwaiter().GetResult();var s=Settings.Defaults();var c=new Camera {Name="Test",Host="192.168.1.20",User="a@b",Password="p:#?&"};c.Validate();s.Cameras.Add(c);s.Pages[0].Slots[0]=c.Id;
  var encrypted=Store.Encode(s);Assert(!Encoding.UTF8.GetString(encrypted).Contains("p:#?&"),"Passwortschutz");var restored=Store.Decode(encrypted);Assert(restored.Cameras[0].Password==c.Password && restored.Pages[0].Slots[0]==c.Id,"Persistenz");
  Assert(new Uri(c.Url()).Host==c.Host && c.Url().Contains("p%3A%23%3F%26"),"URL-Escaping");
  bool invalid=false;try{Search.Number("300.1.2.3");}catch(Exception){invalid=true;}Assert(invalid,"IPv4-Validierung");Assert(Search.Address(Search.Number("192.168.4.254"))=="192.168.4.254","IP-Konvertierung");
  var xml="<Envelope><ProbeMatch><XAddrs>http://192.168.2.4:8899/onvif/device_service</XAddrs><Scopes>onvif://www.onvif.org/name/Feld%201</Scopes></ProbeMatch></Envelope>";var found=Search.Parse(Encoding.UTF8.GetBytes(xml),"1.1.1.1");Assert(found.Count==1 && found[0].Host=="192.168.2.4" && found[0].Name=="Feld 1","ONVIF-Parsing");
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;Assert(Search.PortOpen("127.0.0.1",port,CancellationToken.None).GetAwaiter().GetResult(),"TCP-Erkennung");listener.Stop();Assert(!Search.PortOpen("127.0.0.1",port,CancellationToken.None).GetAwaiter().GetResult(),"Geschlossener Port");
  using(var main=new MainForm(s)) {main.Show();Application.DoEvents();if(args.Length>1)Snapshot(main,args[1]);
   var tabs=Descendants(main).OfType<TabControl>().First();tabs.SelectedIndex=1;Application.DoEvents();Assert(Descendants(tabs.TabPages[1]).OfType<ComboBox>().Count()==9,"Rasterplätze und Bildschirmwahl");
   if(args.Length>1)Snapshot(main,System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[1]),"Raster.png"));
   tabs.SelectedIndex=2;Application.DoEvents();if(args.Length>1)Snapshot(main,System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[1]),"Start.png"));tabs.SelectedIndex=3;Application.DoEvents();if(args.Length>1)Snapshot(main,System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[1]),"Uebertragen.png"));main.Hide();}
  using(var editor=new CameraForm(c,s)){editor.Show();Application.DoEvents();if(args.Length>1)Snapshot(editor,System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[1]),"Kamera.png"));editor.Hide();}
  using(var discovery=new DiscoveryForm()){discovery.Show();Application.DoEvents();if(args.Length>1)Snapshot(discovery,System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[1]),"Suche.png"));discovery.Hide();}
  using(var display=new DisplayForm(s,Screen.AllScreens[0],s.Pages,true)){display.Show();Pump(1200);Assert(Descendants(display).OfType<VideoTile>().Count()==8,"Demoraster");display.SelectView(2);Pump(1200);Assert(Descendants(display).OfType<VideoTile>().Count()==4,"Tabwechsel gibt alte Videos frei");if(args.Length>1)Snapshot(display,System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[1]),"Demo.png"));display.Close();}
  Console.WriteLine("PASS: DPAPI, XML roundtrip, URL escaping, IP validation, ONVIF parser, loopback TCP, UI creation.");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
}













