using Newtonsoft.Json;
using SpotifyWebAPI.SpotifyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyWebAPI
{
    /// <summary>
    /// Spotify Paged Result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Page<T> : BaseModel
    {
        /// <summary>
        /// A link to the Web API endpoint returning the full result of the request.
        /// </summary>
        public string HREF { get; set; }

        /// <summary>
        /// The requested data of type T
        /// </summary>
        public List<T> Items { get; set; }

        /// <summary>
        /// The maximum number of items in the response (as set in the query or by default).
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// URL to the next page of items. (null if none) 
        /// </summary>
        public string Next { get; set; }

        /// <summary>
        /// The offset of the items returned (as set in the query or by default).
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// URL to the previous page of items. (null if none) 
        /// </summary>
        public string Previous { get; set; }

        /// <summary>
        /// The total number of items available to return. 
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// True if this object has another page
        /// </summary>
        public bool HasNextPage { get { return !(Next == null); } }

        /// <summary>
        /// True if this object has a previous page
        /// </summary>
        public bool HasPreviousPage { get { return !(Previous == null); } }

        /// <summary>
        /// default constructor of the page object
        /// </summary>
        public Page()
        {
            this.HREF = null;
            this.Items = new List<T>();
            this.Limit = 20;
            this.Next = null;
            this.Offset = 0;
            this.Previous = null;
            this.Total = 0;
        }

        /// <summary>
        /// If this object has a Next page get it
        /// else
        /// throw new Exception("Next page does not exist.");
        /// </summary>
        /// <returns></returns>
        public async Task<Page<T>> GetNextPage()
        {
            if (!HasNextPage)
                throw new Exception("Next page does not exist.");

            var json = await HttpHelper.Get(Next);

            if (typeof(T) == typeof(Album))
            {
                var obj = JsonConvert.DeserializeObject<page<album>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(Artist))
            {
                var obj = JsonConvert.DeserializeObject<page<artist>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(Track))
            {
                var obj = JsonConvert.DeserializeObject<page<track>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(Playlist))
            {
                var obj = JsonConvert.DeserializeObject<page<playlist>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(PlaylistTrack))
            {
                var obj = JsonConvert.DeserializeObject<page<playlisttrack>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            return null;
        }

        /// <summary>
        /// If this object has a Previous page get it
        /// else
        /// throw new Exception("Previous page does not exist.");
        /// </summary>
        /// <returns></returns>
        public async Task<Page<T>> GetPreviousPage()
        {
            if (!HasPreviousPage)
                throw new Exception("Previous page does not exist.");

            var json = await HttpHelper.Get(Previous);

            if (typeof(T) == typeof(Album))
            {
                var obj = JsonConvert.DeserializeObject<page<album>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(Artist))
            {
                var obj = JsonConvert.DeserializeObject<page<artist>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(Track))
            {
                var obj = JsonConvert.DeserializeObject<page<track>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(Playlist))
            {
                var obj = JsonConvert.DeserializeObject<page<playlist>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            if (typeof(T) == typeof(PlaylistTrack))
            {
                var obj = JsonConvert.DeserializeObject<page<playlisttrack>>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

                return obj.ToPOCO<T>();
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<List<T>> ToList()
        {
            List<T> items = new List<T>();

            Page<T> nextPage = (Page<T>)(object)this;

            if (this.Items.Any())
                items.AddRange(nextPage.Items);

            while (nextPage.HasNextPage)
            {
                nextPage = await nextPage.GetNextPage();

                if (nextPage.Items.Any())
                    items.AddRange(nextPage.Items);
            }

            return (List<T>)(object)items;
        }
    }
}
