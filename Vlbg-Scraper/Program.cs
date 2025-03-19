using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

class Program
{
    static void Main()
    {
        List<Dictionary<string, string>> scrapedData = new List<Dictionary<string, string>>();
        HashSet<string> uniqueEntries = new HashSet<string>(); // Set für eindeutige Einträge basierend auf bestimmten Feldern

        // Starte den Chrome-Browser
        var options = new ChromeOptions();
        //options.AddArgument("--headless"); // Falls du den Browser nicht sichtbar starten willst
        IWebDriver driver = new ChromeDriver(options);

        // URL der Webseite
        string url = @"https://transparenz.vorarlberg.at/tab/medienauftraege"; // Ersetze mit der echten URL
        driver.Navigate().GoToUrl(url);

        // Warte kurz, um sicherzustellen, dass die Daten geladen sind (optional)
        System.Threading.Thread.Sleep(500);

        Scraper(scrapedData, driver, uniqueEntries);

        // Finde das scrollbare Div
        IWebElement scrollableDiv = driver.FindElement(By.ClassName("tabulator-tableholder"));

        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
        int lastHeight = Convert.ToInt32(js.ExecuteScript("return arguments[0].scrollHeight;", scrollableDiv));

        while (true)
        {
            // Scrolle im div#Datatable nach unten
            js.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", scrollableDiv);
            Thread.Sleep(500); // Wartezeit für das Laden neuer Daten

            Scraper(scrapedData, driver, uniqueEntries);

            int newHeight = Convert.ToInt32(js.ExecuteScript("return arguments[0].scrollHeight;", scrollableDiv));
            if (newHeight == lastHeight)
                break; // Falls keine neuen Daten mehr geladen werden, abbrechen

            lastHeight = newHeight;
        }

        int counter = 1;
        // Daten ausgeben
        foreach (var entry in scrapedData)
        {
            Console.WriteLine($"{counter++}.Medium: {entry["Medium"]}, Datum: {entry["Erscheinungsdatum"]}, Enddatum: {entry["Enddatum"]}, Verwendungszweck: {entry["Verwendungszweck"]}, Kampagne: {entry["Kampagne"]}, Werbeentgelt: {entry["Werbeentgelt"]}");
        }

        // Browser schließen
        driver.Quit();
    }

    static void Scraper(List<Dictionary<string, string>> scrapedData, IWebDriver driver, HashSet<string> uniqueEntries)
    {
        // Finde alle "tabulator-row"-Elemente
        var rows = driver.FindElements(By.ClassName("tabulator-row"));
        foreach (var row in rows)
        {
            string hashstring = GenerateRowHash(row);

            if (!uniqueEntries.Contains(hashstring))
            {
                var cells = row.FindElements(By.ClassName("tabulator-cell"));
                if (cells.Count > 5)
                {
                    var rowData = new Dictionary<string, string>
                {
                    { "Medium", cells[0].Text },
                    { "Erscheinungsdatum", cells[1].Text },
                    { "Enddatum", cells[2].Text },
                    { "Verwendungszweck", cells[3].Text },
                    { "Kampagne", cells[4].Text },
                    { "Werbeentgelt", cells[5].Text }
                };

                    uniqueEntries.Add(hashstring);
                    scrapedData.Add(rowData);
                }
            }
        }
    }

    public static string GenerateRowHash(IWebElement row)
    {
        try
        {
            // Extrahiere die Klasse der Zeile (odd/even)
            string rowClass = row.GetAttribute("class");

            // Finde alle "tabulator-cell"-Elemente innerhalb der Zeile
            var cells = row.FindElements(By.ClassName("tabulator-cell"));
            if (cells.Count < 6) return null; // Sicherheitscheck

            // Extrahiere die relevanten Werte aus den Zellen
            string medium = cells[0].Text.Trim();
            string erscheinungsdatum = cells[1].Text.Trim();
            string enddatum = cells[2].Text.Trim();
            string verwendungszweck = cells[3].Text.Trim();
            string kampagne = cells[4].Text.Trim();
            string werbeentgelt = cells[5].Text.Trim();

            // Erstelle den String für den Hash
            string rawData = $"{rowClass}|{medium}|{erscheinungsdatum}|{enddatum}|{verwendungszweck}|{kampagne}|{werbeentgelt}";

            // Erzeuge einen Hash aus den gesammelten Daten
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Hexadezimal darstellen
                }
                return builder.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Hashing: {ex.Message}");
            return null;
        }
    }
}

