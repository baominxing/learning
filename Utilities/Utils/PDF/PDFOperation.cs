﻿using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;

namespace Utils.PDF
{
    /// <summary>
    /// PDF文档操作类
    /// </summary>
    //------------------------------------调用--------------------------------------------
    //PDFOperation pdf = new PDFOperation();
    //pdf.Open(new FileStream(path, FileMode.Create));
    //pdf.SetBaseFont(@"C:\Windows\Fonts\SIMHEI.TTF");
    //pdf.AddParagraph("测试文档（生成时间：" + DateTime.Now + "）", 15, 1, 20, 0, 0);
    //pdf.Close();
    //-------------------------------------------------------------------------------------
    public class PDFOperation
    {
        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public PDFOperation()
        {
            rect = PageSize.A4;
            document = new iTextSharp.text.Document(rect);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">页面大小(如"A4")</param>
        public PDFOperation(string type)
        {
            SetPageSize(type);
            document = new iTextSharp.text.Document(rect);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">页面大小(如"A4")</param>
        /// <param name="marginLeft">内容距左边框距离</param>
        /// <param name="marginRight">内容距右边框距离</param>
        /// <param name="marginTop">内容距上边框距离</param>
        /// <param name="marginBottom">内容距下边框距离</param>
        public PDFOperation(string type, float marginLeft, float marginRight, float marginTop, float marginBottom)
        {
            SetPageSize(type);
            document = new iTextSharp.text.Document(rect, marginLeft, marginRight, marginTop, marginBottom);
        }
        #endregion

        #region 私有字段
        private Font font;
        private Rectangle rect;   //文档大小
        private Document document;//文档对象
        private BaseFont basefont;//字体
        #endregion

        #region 设置字体
        /// <summary>
        /// 设置字体
        /// </summary>
        public void SetBaseFont(string path)
        {
            basefont = BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
        }

        /// <summary>
        /// 设置字体
        /// </summary>
        /// <param name="size">字体大小</param>
        public void SetFont(float size)
        {
            font = new iTextSharp.text.Font(basefont, size);
        }
        #endregion

        #region 设置页面大小
        /// <summary>
        /// 设置页面大小
        /// </summary>
        /// <param name="type">页面大小(如"A4")</param>
        public void SetPageSize(string type)
        {
            switch (type.Trim())
            {
                case "A4":
                    rect = PageSize.A4;
                    break;
                case "A8":
                    rect = PageSize.A8;
                    break;
            }
        }
        #endregion

        #region 实例化文档
        /// <summary>
        /// 实例化文档
        /// </summary>
        /// <param name="os">文档相关信息（如路径，打开方式等）</param>
        public void GetInstance(Stream os)
        {
            PdfWriter.GetInstance(document, os);
        }
        #endregion

        #region 打开文档对象
        /// <summary>
        /// 打开文档对象
        /// </summary>
        /// <param name="os">文档相关信息（如路径，打开方式等）</param>
        public void Open(Stream os)
        {
            GetInstance(os);
            document.Open();
        }
        #endregion

        #region 关闭打开的文档
        /// <summary>
        /// 关闭打开的文档
        /// </summary>
        public void Close()
        {
            document.Close();
        }
        #endregion

        #region 添加段落
        /// <summary>
        /// 添加段落
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="fontsize">字体大小</param>
        public void AddParagraph(string content, float fontsize)
        {
            SetFont(fontsize);
            Paragraph pra = new Paragraph(content, font);
            document.Add(pra);
        }

        /// <summary>
        /// 添加段落
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="fontsize">字体大小</param>
        /// <param name="Alignment">对齐方式（1为居中，0为居左，2为居右）</param>
        /// <param name="SpacingAfter">段后空行数（0为默认值）</param>
        /// <param name="SpacingBefore">段前空行数（0为默认值）</param>
        /// <param name="MultipliedLeading">行间距（0为默认值）</param>
        public void AddParagraph(string content, float fontsize, int Alignment, float SpacingAfter, float SpacingBefore, float MultipliedLeading)
        {
            SetFont(fontsize);
            Paragraph pra = new Paragraph(content, font);
            pra.Alignment = Alignment;
            if (SpacingAfter != 0)
            {
                pra.SpacingAfter = SpacingAfter;
            }
            if (SpacingBefore != 0)
            {
                pra.SpacingBefore = SpacingBefore;
            }
            if (MultipliedLeading != 0)
            {
                pra.MultipliedLeading = MultipliedLeading;
            }
            document.Add(pra);
        }
        #endregion

        #region 添加图片
        /// <summary>
        /// 添加图片
        /// </summary>
        /// <param name="path">图片路径</param>
        /// <param name="Alignment">对齐方式（1为居中，0为居左，2为居右）</param>
        /// <param name="newWidth">图片宽（0为默认值，如果宽度大于页宽将按比率缩放）</param>
        /// <param name="newHeight">图片高</param>
        public void AddImage(string path, int Alignment, float newWidth, float newHeight)
        {
            Image img = Image.GetInstance(path);
            img.Alignment = Alignment;
            if (newWidth != 0)
            {
                img.ScaleAbsolute(newWidth, newHeight);
            }
            else
            {
                if (img.Width > PageSize.A4.Width)
                {
                    img.ScaleAbsolute(rect.Width, img.Width * img.Height / rect.Height);
                }
            }
            document.Add(img);
        }
        #endregion

        #region 添加链接、点
        /// <summary>
        /// 添加链接
        /// </summary>
        /// <param name="Content">链接文字</param>
        /// <param name="FontSize">字体大小</param>
        /// <param name="Reference">链接地址</param>
        public void AddAnchorReference(string Content, float FontSize, string Reference)
        {
            SetFont(FontSize);
            Anchor auc = new Anchor(Content, font);
            auc.Reference = Reference;
            document.Add(auc);
        }

        /// <summary>
        /// 添加链接点
        /// </summary>
        /// <param name="Content">链接文字</param>
        /// <param name="FontSize">字体大小</param>
        /// <param name="Name">链接点名</param>
        public void AddAnchorName(string Content, float FontSize, string Name)
        {
            SetFont(FontSize);
            Anchor auc = new Anchor(Content, font);
            auc.Name = Name;
            document.Add(auc);
        }
        #endregion



        /// <summary>
        /// 上传附件转换成PDF
        /// </summary>
        /// <param name="fileFullPath">文件完整路径</param>
        /// <returns>转换后PDF的URL</returns>
        public static string AttachmentFile2PDF(string fileFullPath, string target)
        {
            string ret = string.Empty;
            try
            {
                string fileExt = Path.GetExtension(fileFullPath).ToLower();
                string filePath = Path.GetFullPath(fileFullPath);
                string pdfFile = fileFullPath + ".pdf";
                string outFile = "";
                // string pdfPath = fileFullPath.Replace(ConfigUtil.GetAppConfig("AttachmentPath"), "").Replace(@"\", @"/") + ".pdf";
                string pdfPath = target.Replace(@"\", @"/") + ".pdf";
                if (!File.Exists(pdfFile))
                {
                    switch (fileExt)
                    {
                        case ".doc":
                        case ".docx":
                            Aspose.Words.Document doc = new Aspose.Words.Document(fileFullPath);
                            doc.Save(pdfFile, Aspose.Words.SaveFormat.Pdf);
                            outFile = pdfPath;
                            break;
                        case ".xls":
                        case ".xlsx":
                            Aspose.Cells.Workbook excel = new Aspose.Cells.Workbook(fileFullPath);
                            excel.Settings.MemorySetting = Aspose.Cells.MemorySetting.MemoryPreference;
                            excel.Settings.AutoCompressPictures = true;
                            excel.Settings.EnableMacros = false;
                            Aspose.Cells.PdfSaveOptions saveOptions = new Aspose.Cells.PdfSaveOptions(Aspose.Cells.SaveFormat.Pdf);
                            saveOptions.OnePagePerSheet = true;
                            saveOptions.PdfCompression = Aspose.Cells.Rendering.PdfCompressionCore.Flate;
                            saveOptions.PrintingPageType = Aspose.Cells.PrintingPageType.IgnoreBlank;
                            excel.Save(pdfFile, saveOptions);
                            outFile = pdfPath;
                            break;
                        case ".ppt":
                        case ".pptx":
                            Aspose.Slides.Presentation ppt = new Aspose.Slides.Presentation(fileFullPath);
                            ppt.Save(pdfFile, Aspose.Slides.Export.SaveFormat.Pdf);
                            outFile = pdfPath;
                            break;
                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".bmp":
                            try
                            {
                                Aspose.Pdf.Generator.Pdf pdf = new Aspose.Pdf.Generator.Pdf();
                                Aspose.Pdf.Generator.Section sec = pdf.Sections.Add();
                                Aspose.Pdf.Generator.Image image = new Aspose.Pdf.Generator.Image(sec);
                                sec.Paragraphs.Add(image);
                                image.ImageInfo.File = fileFullPath;
                                // image.ImageInfo.ImageFileType = Aspose.Pdf.Generator.ImageFileType.Jpeg;
                                pdf.Save(pdfFile);
                                outFile = pdfPath;
                            }
                            catch (Exception ex)
                            {
                                if (File.Exists(pdfFile))
                                {
                                    File.Delete(pdfFile);
                                }
                                throw ex;
                            }


                            break;
                        case ".pdf":
                            pdfPath = pdfPath.Substring(0, pdfPath.Length - 4);
                            outFile = pdfPath;
                            break;
                    }
                }

                return outFile;
            }
            catch (System.Exception ex)
            {
                ret = string.Empty;
               // LogWriter.GetInstance().ErrOut(ex.ToString());
            }
            return ret;
        }

    }
}