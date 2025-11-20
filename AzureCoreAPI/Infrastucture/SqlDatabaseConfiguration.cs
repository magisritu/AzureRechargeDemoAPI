using System.ComponentModel.DataAnnotations;

namespace AzureCoreAPI.Infrastucture
{
    internal class SqlDatabaseConfiguration
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
