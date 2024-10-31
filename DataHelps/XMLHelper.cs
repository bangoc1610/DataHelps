using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace DataHelps
{
    /// <summary>
    /// Class hỗ trợ thao tác lấy dữ liệu từ file XML cấu hình.
    /// </summary>
    public class XMLHelper
    {
        public XMLHelper() { }

        /// <summary>
        /// Lấy nội dung của một node XML từ tên phần cấu hình mặc định.
        /// </summary>
        public static string GetXMLNodeText(string sectionName, string nodeName)
        {
            return GetXMLNodeText(DBHelper.XMLFilePath, sectionName, nodeName);
        }

        /// <summary>
        /// Lấy nội dung của một node XML từ file XML chỉ định.
        /// </summary>
        /// <param name="xmlFileName">Đường dẫn tới file XML</param>
        /// <param name="sectionName">Tên phần cấu hình trong file XML</param>
        /// <param name="nodeName">Tên node cần lấy dữ liệu</param>
        public static string GetXMLNodeText(string xmlFileName, string sectionName, string nodeName)
        {
            xmlFileName = string.IsNullOrEmpty(xmlFileName) ? DBHelper.XMLFilePath : xmlFileName;

            // Xác định đường dẫn đầy đủ của file XML
            string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase)?.Replace("file:\\", "");
            string fullPath = Path.Combine(directoryPath, xmlFileName);

            // Tải tài liệu XML và lấy node theo tên phần và tên node
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(fullPath);
            XmlNode sectionNode = xmlDocument.GetElementsByTagName(sectionName).Item(0);
            XmlNode node = sectionNode?.SelectSingleNode(nodeName);

            return node?.InnerText;
        }
    }
}
