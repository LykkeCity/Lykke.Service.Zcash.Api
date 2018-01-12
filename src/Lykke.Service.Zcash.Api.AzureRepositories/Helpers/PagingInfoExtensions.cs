using System;
using System.Collections.Generic;
using System.Net;

namespace Lykke.AzureStorage.Tables.Paging
{
    public static class PagingInfoExtensions
    {
        public const char SEPARATOR = '.';

        public static void Decode(this PagingInfo self, string continuation = null)
        {
            if (!string.IsNullOrWhiteSpace(continuation))
            {
                var parts = WebUtility.UrlDecode(continuation).Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    throw new InvalidOperationException("Invalid continuation");
                }
                else
                {
                    self.CurrentPage = int.Parse(parts[0]);
                    self.NavigateToPageIndex = int.Parse(parts[1]);
                }

                if (parts.Length > 2)
                {
                    self.NextPage = parts[2];
                }
            }

            return self;
        }

        public static string Encode(this PagingInfo self)
        {
            if (self == null)
            {
                return null;
            }

            var parts = new List<string>
            {
                self.CurrentPage.ToString(),
                self.NavigateToPageIndex.ToString()
            };

            if (!string.IsNullOrWhiteSpace(self.NextPage))
            {
                parts.Add(self.NextPage);
            }

            var value = string.Join(SEPARATOR, parts);

            return WebUtility.UrlEncode(value);
        }
    }
}
