using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BridgePointsCounter
{
    class GoogleWorksheet
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Bridge";
        readonly string spreadsheetId = "17OsMxxxxxxxxxxxxxxxxxxxxxxxxxxxyhwjU";
        UserCredential credential;
        SheetsService service;

        public GoogleWorksheet()
        {
            using (var stream = new FileStream("client_secret_1015xxxxxxxxxxxxxxxxxxxxxxxxxxxi5eq9.apps.googleusercontent.com.json", FileMode.Open, FileAccess.ReadWrite))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        public void Save(List<List<object>> data)
        {
            Tuple<int, bool> lastGameParams = GetLastGameParams();
            int newGameIndex = lastGameParams.Item1;
            newGameIndex += lastGameParams.Item2 ? 1 : 0;
            string range = string.Format("Arkusz1!A{0}:I{1}", newGameIndex * 3 + 1, newGameIndex * 3 + 2);

            UpdateSpreadsheet(range, data);

            bool wasFinished = true;
            if (data[0][8].Equals(string.Empty) && data[1][8].Equals(string.Empty))
                wasFinished = false;
            List<List<object>> newGameParams = new List<List<object>>() { new List<object>() { newGameIndex.ToString(), wasFinished.ToString() } };
            string range2 = "Arkusz1!A2:B2";
            UpdateSpreadsheet(range2, newGameParams);
        }
        private void UpdateSpreadsheet(string range, List<List<object>> data)
        {
            ValueRange valueRange = new ValueRange();
            valueRange.MajorDimension = "ROWS";

            valueRange.Values = new List<IList<object>>(data);

            SpreadsheetsResource.ValuesResource.UpdateRequest update = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            UpdateValuesResponse result = update.Execute();
        }

        public List<List<string>> Load()
        {
            Tuple<int, bool> lastGameParams = GetLastGameParams();

            string range = string.Format("Arkusz1!B{0}:I{1}", lastGameParams.Item1 * 3 + 1, lastGameParams.Item1 * 3 + 2);
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            return RefactorResponse(response.Values);
        }

        public bool IsPreviousGameFinished()
        {
            Tuple<int, bool> lastGameParams = GetLastGameParams();
            return lastGameParams.Item2;
        }

        public List<Tuple<string, string>> LoadRecentResults()
        {
            Tuple<int, bool> lastGameParams = GetLastGameParams();
            int lastFullGameId = lastGameParams.Item2 ? lastGameParams.Item1 : lastGameParams.Item1 - 1;
            int operatedGameId = lastFullGameId;
            List<List<List<string>>> RecentScores = new List<List<List<string>>>();
            while (operatedGameId > 0 && lastFullGameId - operatedGameId < 5)
            {
                string range = string.Format("Arkusz1!B{0}:I{1}", operatedGameId * 3 + 1, operatedGameId * 3 + 2);
                SpreadsheetsResource.ValuesResource.GetRequest request =
                        service.Spreadsheets.Values.Get(spreadsheetId, range);

                ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                RecentScores.Add(RefactorResponse(response.Values));
                operatedGameId--;
            }
            return Normalize(CalculateResults(RecentScores));
        }

        private List<Tuple<string, string>> Normalize(List<Tuple<string, string>> results)
        {
            while (results.Count < 5) results.Add(null);
            return results;
        }

        private List<Tuple<string, string>> CalculateResults(List<List<List<string>>> recentScores)
        {
            List<Tuple<string, string>> results = new List<Tuple<string, string>>();
            foreach(List<List<string>> game in recentScores)
            {
                int Lsum = 0;
                int Rsum = 0;
                foreach(string points in game[0])
                {
                    if (int.TryParse(points, out int p))
                        Lsum += p;
                }
                foreach (string points in game[1])
                {
                    if (int.TryParse(points, out int p))
                        Rsum += p;
                }
                int difference = Lsum - Rsum;
                if (difference > 0)
                    results.Add(new Tuple<string, string>("My", difference.ToString()));
                else if (difference < 0)
                    results.Add(new Tuple<string, string>("Oni", (-difference).ToString()));
                else
                    results.Add(new Tuple<string, string>("Remis", "0"));
            }
            return results;
        }

        private List<List<string>> RefactorResponse(IList<IList<object>> oldList)
        {
            if (oldList.Count == 1)
                oldList.Add(new List<Object>());
            List<List<string>> refactoredList = new List<List<string>>();
            for (int i = 0; i < 2; i++)
            {
                refactoredList.Add(new List<string>());
                for (int j = 0; j < 8; j++)
                {
                    string element;
                    element = oldList[i].Count > j ? (string)oldList[i][j] : string.Empty;
                    refactoredList[i].Add(element);
                }
            }
            return refactoredList;
        }

        private Tuple<int, bool> GetLastGameParams()
        {
            string range = "Arkusz1!A2:B2";

            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            try
            {
                return new Tuple<int, bool>(int.Parse((string)values[0][0]), bool.Parse((string)values[0][1]));
            }
            catch (Exception)
            {
                Console.WriteLine("No data found.");
            }
            throw new ArgumentException();
        }
    }
}
