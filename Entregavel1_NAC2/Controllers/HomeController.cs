using Dapper;
using Entregavel1_NAC2.Model;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Entregavel2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly string _connectionString = "Server=localhost;database=fiap;User=root;Password=123";
        private readonly string _redisConnection = "localhost:6379";
        private const string CacheKey = "get-vehicles";

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var redis = ConnectionMultiplexer.Connect(_redisConnection);
            IDatabase db = redis.GetDatabase();

            string cachedValue = await db.StringGetAsync(CacheKey);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                return Ok(JsonConvert.DeserializeObject<List<Vehicle>>(cachedValue));
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT Id, Brand, Model, Year, Plate FROM Vehicle;";
            var vehicles = (await connection.QueryAsync<Vehicle>(sql));

            await db.StringSetAsync(CacheKey, JsonConvert.SerializeObject(vehicles), TimeSpan.FromSeconds(20));

            return Ok(vehicles);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Vehicle vehicle)
        {
            if (vehicle == null)
                return BadRequest("Dados inválidos");

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"INSERT INTO Vehicle (Brand, Model, Year, Plate) 
                           VALUES (@Brand, @Model, @Year, @Plate);
                           SELECT LAST_INSERT_ID();";

            int newId = await connection.ExecuteScalarAsync<int>(sql, vehicle);
            vehicle.Id = newId;

            var redis = ConnectionMultiplexer.Connect(_redisConnection);
            await redis.GetDatabase().KeyDeleteAsync(CacheKey);

            return Ok(vehicle);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vehicle vehicle)
        {
            if (vehicle == null || id != vehicle.Id)
                return BadRequest("Dados inválidos");

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"UPDATE Vehicle 
                           SET Brand = @Brand, Model = @Model, Year = @Year, Plate = @Plate 
                           WHERE Id = @Id;";

            int rows = await connection.ExecuteAsync(sql, vehicle);

            if (rows == 0)
                return NotFound("Veículo não encontrado");

            var redis = ConnectionMultiplexer.Connect(_redisConnection);
            await redis.GetDatabase().KeyDeleteAsync(CacheKey);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "DELETE FROM Vehicle WHERE Id = @Id;";
            int rows = await connection.ExecuteAsync(sql, new { Id = id });

            if (rows == 0)
                return NotFound("Veículo não encontrado");

            var redis = ConnectionMultiplexer.Connect(_redisConnection);
            await redis.GetDatabase().KeyDeleteAsync(CacheKey);

            return NoContent();
        }
    }
}