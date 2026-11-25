using iTextSharp.text;
using iTextSharp.text.pdf;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Services
{
    public static class PdfReportBuilder
    {
        private static BaseFont LoadFont(string webRootPath)
        {
            var candidates = new[]
            {
                Path.Combine(webRootPath, "fonts", "DejaVuSans.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException(
                    "Brak czcionki. Wgraj DejaVuSans.ttf do wwwroot/fonts/.");

            return BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        }

        private static string Fmt(TimeSpan t) =>
            $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

        private static PdfPCell HeaderCell(string text, Font font) => new(new Phrase(text, font))
        {
            BackgroundColor = new BaseColor(27, 110, 194),
            Padding = 6,
            HorizontalAlignment = Element.ALIGN_LEFT
        };

        private static PdfPCell BodyCell(string? text, Font font) => new(new Phrase(text ?? "", font))
        {
            Padding = 5
        };

        public static byte[] WorkTimeReport(List<WorkTimeViewModel> report, DateTime month, string webRoot)
        {
            var bf = LoadFont(webRoot);
            var title = new Font(bf, 16, Font.BOLD);
            var head = new Font(bf, 10, Font.BOLD, BaseColor.White);
            var body = new Font(bf, 10);
            var bold = new Font(bf, 10, Font.BOLD);

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            doc.Add(new Paragraph("Raport czasu pracy", title));
            doc.Add(new Paragraph($"Miesiąc: {month:yyyy-MM}   |   Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}", body)
            { SpacingAfter = 12 });

            var table = new PdfPTable(new float[] { 3f, 3f, 2f }) { WidthPercentage = 100 };
            table.HeaderRows = 1;
            table.AddCell(HeaderCell("Pracownik", head));
            table.AddCell(HeaderCell("Stanowisko", head));
            table.AddCell(HeaderCell("Czas pracy", head));

            foreach (var item in report)
            {
                table.AddCell(BodyCell($"{item.User.FirstName} {item.User.LastName}", body));
                table.AddCell(BodyCell(item.User.Position, body));
                table.AddCell(BodyCell(Fmt(item.WorkTime), body));
            }

            var total = report.Aggregate(TimeSpan.Zero, (t, i) => t + i.WorkTime);
            var totalLabel = new PdfPCell(new Phrase("Łącznie", bold)) { Colspan = 2, Padding = 5 };
            table.AddCell(totalLabel);
            table.AddCell(new PdfPCell(new Phrase(Fmt(total), bold)) { Padding = 5 });

            doc.Add(table);
            doc.Close();
            return ms.ToArray();
        }

        public static byte[] LeaveRequestReport(List<LeaveRequest> requests, string webRoot)
        {
            var bf = LoadFont(webRoot);
            var title = new Font(bf, 16, Font.BOLD);
            var head = new Font(bf, 9, Font.BOLD, BaseColor.White);
            var body = new Font(bf, 9);

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4.Rotate(), 30, 30, 30, 30);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            doc.Add(new Paragraph("Raport wniosków urlopowych", title));
            doc.Add(new Paragraph($"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}   |   Liczba wniosków: {requests.Count}", body)
            { SpacingAfter = 12 });

            var table = new PdfPTable(new float[] { 2.5f, 2f, 2f, 1.8f, 1.5f, 1.5f, 3f }) { WidthPercentage = 100 };
            table.HeaderRows = 1;
            foreach (var h in new[] { "Pracownik", "Stanowisko", "Status", "Rodzaj", "Od", "Do", "Uzasadnienie" })
                table.AddCell(HeaderCell(h, head));

            foreach (var r in requests)
            {
                table.AddCell(BodyCell($"{r.User.FirstName} {r.User.LastName}", body));
                table.AddCell(BodyCell(r.User.Position, body));
                table.AddCell(BodyCell(r.Status, body));
                table.AddCell(BodyCell(r.LeaveType, body));
                table.AddCell(BodyCell(r.StartDate.ToString("dd.MM.yyyy"), body));
                table.AddCell(BodyCell(r.EndDate.ToString("dd.MM.yyyy"), body));
                table.AddCell(BodyCell(r.Reason, body));
            }

            doc.Add(table);
            doc.Close();
            return ms.ToArray();
        }
    }
}