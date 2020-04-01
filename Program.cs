using System;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using MoreLinq;
using Tweetinvi.Parameters;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;
using ModuleCrier2.Models;

namespace ModuleCrier2
{
    class Program
    {
        private static string TwitterApiKey = Environment.GetEnvironmentVariable("TWITTER_API_KEY");
        private static string TwitterApiSecretKey = Environment.GetEnvironmentVariable("TWITTER_API_TOKEN");
        private static string TwitterAccessSecret = Environment.GetEnvironmentVariable("TWITTER_ACCESS_SECRET");
        private static string TwitterAccessToken = Environment.GetEnvironmentVariable("TWITTER_ACCESS_TOKEN");

        static void Main(string[] args)
        {
            GetTwitterUser()
                .GetLatestTweet()
                .GetDriveThruProductID()
                .GetNextTitleSinceThen(0, new HttpClient())
                .SendTweet();
        }

        private static IAuthenticatedUser GetTwitterUser(){
            var userCredentials = Auth.CreateCredentials(TwitterApiKey,
             TwitterApiSecretKey, TwitterAccessToken, TwitterAccessSecret);
            return User.GetAuthenticatedUser(userCredentials);
        }
    }

    internal static class ProjectExtensions{
        public static (IAuthenticatedUser, ITweet) GetLatestTweet(this IAuthenticatedUser user){
           return (user, user.GetHomeTimeline()
            .FirstOrDefault());
        }

        public static (IAuthenticatedUser, string) GetDriveThruProductID(this (IAuthenticatedUser, ITweet) data){
            var tweet = data.Item2;
            var user = data.Item1;
            var defaultValue = "17552";
            //https://www.drivethrurpg.com/product/193622
            if(tweet == null){
                return (user, defaultValue);
            }
            var pattern = @"https://www.drivethrurpg.com/product/(\d+)";

var temp = Regex.Matches(tweet.Urls.FirstOrDefault()?.ExpandedURL ?? string.Empty, pattern);
            return (user, Regex.Matches(tweet.Urls.FirstOrDefault()?.ExpandedURL ?? string.Empty, pattern)
                .FirstOrDefault()
                .Groups
                .Values
                .LastOrDefault()
                ?.Captures
                .FirstOrDefault()
                ?.Value ?? defaultValue);

        }
    
        public static (IAuthenticatedUser, NewDriveThruListing) GetNextTitleSinceThen(
         this (IAuthenticatedUser, string) data, int index,
         HttpClient client, DriveThruAPIResults previousListings = null)
        {
            var user = data.Item1;
            var id = data.Item2;
            var response = client.GetAsync(GetUrl(index)).Result;
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().Result;
            var newListings = DriveThruAPIResults.FromJson(responseBody);
            var itemIndex = newListings.Results
                .Select(_ => _.ProductsId.ToString())
                .ToList()
                .IndexOf(id);

            if (itemIndex > 0)
            {
                return (user, newListings.Results[itemIndex - 1]);
            }
            else if (itemIndex == 0) 
            {
                if (previousListings == null)
                {
                    throw new Exception($"Could not find any new listings. Search ID: {id}");
                }
                //This means the one I want is the last of the previous set.
                return (user, previousListings.Results.Last());
            }
            else // if (itemIndex == -1)
            {
                index += newListings.Results.Count();
                return data.GetNextTitleSinceThen(index, client, newListings);
            }
        }

        public static string GetUrl(int index)
        {
            var queryParams = new Dictionary<string, string>{
                {"manufacturers_id", "44"},
                {"filters", "0_0_0_44294_0"},
                {"index", index.ToString()}
            };
            return QueryHelpers.AddQueryString("https://www.drivethrurpg.com/api/products/list/newest", queryParams);
        }
    
        public static void SendTweet(this (IAuthenticatedUser, NewDriveThruListing) data) {
            var user = data.Item1;
            var listing = data.Item2;
            var tweet = $"New D&D POD Listing: {listing.ProductsName}{Environment.NewLine}";
            tweet += $"{Environment.NewLine}#Dnd #DTRPG #DungeonsAndDragons{Environment.NewLine}";
            tweet += $"https://www.drivethrurpg.com/product/{listing.ProductsId}?affiliate_id=381232";

            user.PublishTweet(tweet);
        }

        public static void Debug(this NewDriveThruListing data){
            Console.WriteLine(data.ToString());
        }
    }
}
