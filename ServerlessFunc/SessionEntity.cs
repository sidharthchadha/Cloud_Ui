using Azure;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ITableEntity = Azure.Data.Tables.ITableEntity;

namespace ServerlessFunc
{
    public class SessionEntity : ITableEntity
    {
        public const string PartitionKeyName = "SessionEntityPartitionKey";

        public SessionEntity(SessionData sessionData)
        {
            PartitionKey = PartitionKeyName;
            RowKey = Guid.NewGuid().ToString();
            Id = RowKey;
            SessionId = sessionData.SessionId;
            HostUserName = sessionData.HostUserName;
            Timestamp = DateTime.Now;
            Tests = sessionData.Tests;
            Students = sessionData.Students;
        }

        public SessionEntity() : this(null) { }

        /// <summary>
        /// To store session Id.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("SessionId")]
        public string SessionId { get; set; }


        /// <summary>
        /// To store Unique id for the entry.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("Id")]
        public string Id { get; set; }


        /// <summary>
        /// To store the host user name.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("HostUserName")]
        public string HostUserName { get; set; }

        [JsonInclude]
        [JsonPropertyName("PartitionKey")]
        public string PartitionKey { get; set; }

        [JsonInclude]
        [JsonPropertyName("RowKey")]
        public string RowKey { get; set; }

        /// <summary>
        /// To store start Timestamp of the session.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("Timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// To store the tests done in session
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("Tests")]
        public List<string> Tests { get; set; }

        /// <summary>
        /// To store the list of studnets joined in session
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("Tests")]
        public List<string> Students { get; set; }

        [JsonIgnore]
        public ETag ETag { get; set; }
    }

    public class SessionData
    {
        public string HostUserName { get; set; }
        public string SessionId { get; set; }
        public List<string> Tests { get; set; }
        public List<string> Students { get; set; }
    }
}
