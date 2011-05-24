using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace RDLCTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string rdlc;
            if(args.Length>0)
                rdlc = args[0];
            else
                rdlc = @"..\..\TestReport.rdlc";
            if (!File.Exists(rdlc))
            {
                _("Cannot read input file " + rdlc + ".");
                return;
            }
            //_(File.ReadAllText(rdlc));
            XmlReader xml = XmlReader.Create(rdlc);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("x", "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition");
            XPathDocument doc = new XPathDocument(xml);
            XPathNavigator nav = doc.CreateNavigator();


            _("Getting textboxes...");

            nav.MoveToRoot();
            XPathNodeIterator iterator = nav.Select("/x:Report/x:Body/x:ReportItems/x:Textbox", nsmgr);

            List<string> tbList = new List<string>();

            // Iterate on the node set
            try
            {
                string sTbName = "";
                while (iterator.MoveNext())
                {
                    sTbName = iterator.Current.GetAttribute("Name", "");
                    if (sTbName[0] == 'x')
                    {
                        Console.WriteLine(" - Found TextBox: " + sTbName);
                        tbList.Add(sTbName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            _("Creating new class file...");
            string sClass = "";
            foreach (string tb in tbList)
            {
                sClass += "        p[\"" + tb + "\"] = \"\";\r\n";

            }
            File.WriteAllText(rdlc.Replace("rdlc", "txt"), sClass);

            _("Checking for parameters...");
            iterator = nav.Select("/x:Report/x:ReportParameters/x:ReportParameter", nsmgr);
            try
            {
                string sPaName = "";
                while (iterator.MoveNext())
                {
                    sPaName = iterator.Current.GetAttribute("Name", "");
                    if (sPaName[0] == 'x')
                    {
                        Console.WriteLine(" - Found Parameter: " + sPaName + ", removing from list...");
                        tbList.Remove(sPaName);
                    }
                    else
                    {
                        _(" - Found other parameter.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            nav = null;
            xml.Close();


            _("Creating additional parameters...");
            XmlDocument xDoc = new XmlDocument();
            XmlNode newNode,dtNode,prNode;
            XmlAttribute newAttr;
            xDoc.Load(rdlc);
            var parNode = xDoc.SelectSingleNode("/x:Report/x:ReportParameters", nsmgr);
            if (parNode == null)
            {
                var rp = xDoc.CreateElement("ReportParameters", xDoc.DocumentElement.NamespaceURI);
                xDoc.SelectSingleNode("/x:Report", nsmgr).AppendChild(rp);
                parNode = rp;
            }
            foreach (string tb in tbList)
            {
                newNode = xDoc.CreateElement("ReportParameter", parNode.NamespaceURI);
                newAttr = xDoc.CreateAttribute("Name");
                newAttr.Value = tb;
                newNode.Attributes.Append(newAttr);
                
                dtNode = xDoc.CreateElement("DataType", parNode.NamespaceURI);
                dtNode.InnerText = "String";
                prNode = xDoc.CreateElement("Prompt", parNode.NamespaceURI);
                prNode.InnerText = tb;
                newNode.AppendChild(dtNode);
                newNode.AppendChild(prNode);

                parNode.AppendChild(newNode);
            }

            _("Setting values...");//=Parameters!ReportParameter1.Value
            var tbNodes = xDoc.SelectNodes("/x:Report/x:Body/x:ReportItems/x:Textbox", nsmgr);
            foreach (XmlNode node in tbNodes)
            {
                if(node.Attributes["Name"].Value[0] == 'x')
                    node.SelectSingleNode("x:Paragraphs/x:Paragraph/x:TextRuns/x:TextRun/x:Value", nsmgr).InnerText =
                    "=Parameters!" + node.Attributes["Name"].Value + ".Value";
            }

            _("Saving file...");
            xDoc.Save(rdlc);

            _("\nDone!");
        }
        
        static void _(string s) { Console.WriteLine(s); }
    }
}
