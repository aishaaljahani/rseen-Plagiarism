using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;


using A = DocumentFormat.OpenXml.Drawing;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace Plagiarism.Models
{
    public class PlagiarismUtil
    {
        PlagiarismEntities db = new PlagiarismEntities();
        public string OpenWordDocument(string file)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(file, false))
            {
                const string wordmlNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                StringBuilder textBuilder = new StringBuilder();
                using (StreamReader sr = new StreamReader(wordDocument.MainDocumentPart.GetStream()))
                {
                    // Manage namespaces to perform XPath queries.  
                    NameTable nt = new NameTable();
                    XmlNamespaceManager nsManager = new XmlNamespaceManager(nt);
                    nsManager.AddNamespace("w", wordmlNamespace);

                    // Get the document part from the package.  
                    // Load the XML in the document part into an XmlDocument instance.  
                    XmlDocument xdoc = new XmlDocument(nt);
                    xdoc.Load(wordDocument.MainDocumentPart.GetStream());

                    XmlNodeList paragraphNodes = xdoc.SelectNodes("//w:p", nsManager);
                    foreach (XmlNode paragraphNode in paragraphNodes)
                    {
                        XmlNodeList textNodes = paragraphNode.SelectNodes(".//w:t", nsManager);
                        foreach (System.Xml.XmlNode textNode in textNodes)
                        {
                            textBuilder.Append(textNode.InnerText);
                        }
                        textBuilder.Append(Environment.NewLine);
                    }
                }
                string result = textBuilder.ToString();
                return result;
            }
        }
        public string OpenSpreadsheetDocument(string file)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(file, false))
            {
                StringBuilder textBuilder = new StringBuilder();
                SharedStringTable sharedStringTable = document.WorkbookPart.SharedStringTablePart.SharedStringTable;
                string cellValue = null;

                foreach (WorksheetPart worksheetPart in document.WorkbookPart.WorksheetParts)
                {
                    foreach (SheetData sheetData in worksheetPart.Worksheet.Elements<SheetData>())
                    {
                        if (sheetData.HasChildren)
                        {
                            foreach (Row row in sheetData.Elements<Row>())
                            {
                                foreach (Cell cell in row.Elements<Cell>())
                                {
                                    cellValue = cell.InnerText;

                                    if (cell.DataType == CellValues.SharedString)
                                    {
                                        textBuilder.Append(sharedStringTable.ElementAt(Int32.Parse(cellValue)).InnerText);
                                    }
                                    else
                                    {
                                        textBuilder.Append(cellValue);
                                    }
                                }
                            }
                        }
                    }
                }
                document.Close();
                string result = textBuilder.ToString();
                return result;
            }
        }
        public string OpenPresentationDocument(string file)
        {
            using (PresentationDocument presentationDocument = PresentationDocument.Open(file, false))
            {
                StringBuilder textBuilder = new StringBuilder();
                int numberOfSlides = 0;
                // Get the presentation part of document.
                PresentationPart presentationPart = presentationDocument.PresentationPart;
                // Get the slide count from the SlideParts.
                if (presentationPart != null)
                {
                    numberOfSlides = presentationPart.SlideParts.Count();
                }
                //read text from all slides
                for (int i = 0; i < numberOfSlides; i++)
                {
                    // Get the relationship ID of the first slide.
                    PresentationPart part = presentationDocument.PresentationPart;
                    OpenXmlElementList slideIds = part.Presentation.SlideIdList.ChildElements;

                    string relId = (slideIds[i] as SlideId).RelationshipId;

                    // Get the slide part from the relationship ID.
                    SlidePart slide = (SlidePart)part.GetPartById(relId);

                    // Build a StringBuilder object.
                    StringBuilder paragraphText = new StringBuilder();

                    // Get the inner text of the slide:
                    IEnumerable<A.Text> texts = slide.Slide.Descendants<A.Text>();
                    foreach (A.Text text in texts)
                    {
                        paragraphText.Append(text.Text);
                    }
                    textBuilder.Append(paragraphText.ToString());
                }
                string result = textBuilder.ToString();
                return result;
            }
        }
        public string OpenPdfDocument(string file)
        {
            using (PdfReader reader = new PdfReader(file))
            {
                StringBuilder text = new StringBuilder();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                }

                string result = text.ToString();
                return result;
            }
        }
        public bool BMSearcher(string text, string find)
        {
            CIBMSearcher BMS = new CIBMSearcher(find, true);
            int index = BMS.Search(text, 0);
            if (index >= 0)
            {
                return true;
            }
            return false;
        }
        public int BMSearcherCount(string text, string find)
        {
            CIBMSearcher BMS = new CIBMSearcher(find, true);
            int index = BMS.Search(text, 0);
            int counter = 0;
            while (index >= 0)
            {
                counter++;
                index = BMS.Search(text, index + find.Length);
            }
            return counter;
        }
        public int SearchPopularWordsCount(string text, string PopularWordsFile)
        {
            int popularWordsCount = 0;
            string[] popularWords = System.IO.File.ReadAllLines(PopularWordsFile);
            foreach (string word in popularWords)
            {
                popularWordsCount += BMSearcherCount(text, word);
            }
            //List<string> textWords = text.Split(' ').ToList();
            //popularWordsCount = textWords.Where(x => popularWords.Contains(x)).Count();
            return popularWordsCount;
        }
        public string RemovePopularWords(string text, string PopularWordsFile)
        {
            string[] popularWords = System.IO.File.ReadAllLines(PopularWordsFile);
            foreach (string word in popularWords)
            {
                text = text.Replace(word, "");
            }
            return text;
        }
        public string SearchSimilarity(string text, string PopularWordsFile)
        {
            //Remove the popular words from uploaded file
            string extractedText = RemovePopularWords(text, PopularWordsFile);
            double originalLength = extractedText.Length;

            //split uploaded file into sentences
            string[] extractedTextSentences = extractedText.Split('.');
            int emptyStringLenght = extractedTextSentences.Where(x => string.IsNullOrEmpty(x)).Count();

            string matchedText = "";
            string matchedDetail = "";
            var dbDocuments = db.Documents;
            
            foreach (Document document in dbDocuments)
            {
                string existingProject = RemovePopularWords(document.Abstract, PopularWordsFile);
                string report = "";
                for (int i = 0; i < extractedTextSentences.Length; i++)
                {
                    if (!string.IsNullOrEmpty(extractedTextSentences[i]))
                        if (BMSearcher(existingProject, extractedTextSentences[i]))
                        {
                            matchedText += extractedTextSentences[i] + ".";
                            report += extractedTextSentences[i] + ".";
                            extractedTextSentences[i] = "";
                        }
                }
                if (!string.IsNullOrEmpty(report)) {
                    matchedDetail += "Document %%% is " + Math.Round((((double)report.Length + emptyStringLenght - 1) / originalLength) * 100, 2) + "% similar with document " + document.FilePath.Split('\\').Last() + "¬";
                }
            }
            if(string.IsNullOrEmpty(matchedDetail))
                matchedDetail += "System did not find any similarity.";
            double similarity = Math.Ceiling(Math.Round((((double)matchedText.Length + emptyStringLenght - 1) / originalLength) * 100, 2));
            if (similarity < 0)
                similarity = 0;
            //matchedDetail += "(Document Total Similarity is " + similarity + "%)";
            return matchedDetail;
        }
    }
}
