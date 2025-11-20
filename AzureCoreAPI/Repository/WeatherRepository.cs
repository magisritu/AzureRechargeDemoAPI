using AzureCoreAPI.Entity;
using AzureCoreAPI.Infrastucture;
using Microsoft.EntityFrameworkCore;

namespace AzureCoreAPI.Repository
{
    public interface IWeatherRepository
    {
        Task<IEnumerable<Weather>> GetWeathers();
        Task<IEnumerable<Weather>> AddWeather(Weather weather);
        Task<IEnumerable<Weather>> UpdateWeather(Weather weather);
        Task<IEnumerable<Weather>> UpdateWeatherImage(int weatherId, string url);
        Task<Weather> GetWeathersById(int id);
        bool Login(string email, string password);

    }
    public class WeatherRepository: IWeatherRepository
    {
        private readonly MyDbContext _context;
        public WeatherRepository(MyDbContext context) {
            _context = context;
        }

        public async Task<IEnumerable<Weather>> GetWeathers()
        {
            return await _context.Weathers.AsNoTracking().OrderByDescending(w => w.Date).ToListAsync();
        }
        public async Task<IEnumerable<Weather>> AddWeather(Weather weather)
        {
            await _context.AddAsync(weather);
            await _context.SaveChangesAsync();
            return await _context.Weathers.AsNoTracking().OrderByDescending(w => w.Date).ToListAsync();
        }
        public async Task<IEnumerable<Weather>> UpdateWeather(Weather weather)
        {
            var existingWeather = await _context.Weathers.FirstOrDefaultAsync(w => w.Date == weather.Date);
            if (existingWeather == null)
                throw new KeyNotFoundException("Weather record not found.");

            existingWeather.TemperatureC = weather.TemperatureC;
            existingWeather.TemperatureF = weather.TemperatureF;
            existingWeather.Summary = weather.Summary;

            _context.Weathers.Update(existingWeather);
            await _context.SaveChangesAsync();

            return await _context.Weathers.AsNoTracking().OrderByDescending(w => w.Date).ToListAsync();
        }

        public async Task<IEnumerable<Weather>> UpdateWeatherImage(int weatherId, string url)
        {
            var existingWeather = await _context.Weathers.FirstOrDefaultAsync(w => w.ID == weatherId);
            if (existingWeather == null)
                throw new KeyNotFoundException("Weather record not found.");

            existingWeather.WeatherImageUrl = url;

            _context.Weathers.Update(existingWeather);
            await _context.SaveChangesAsync();

            return await _context.Weathers.AsNoTracking().OrderByDescending(w => w.Date).ToListAsync();
        }

        public async Task<Weather> GetWeathersById(int id)
        {
            return await _context.Weathers.Where(w => w.ID == id).AsNoTracking().FirstAsync();
        }

        public bool Login(string email, string password)
        {
            try
            {
                var user = _context.UserDetails.Where(w => w.Email == email.Trim() && w.Password == password.Trim()).First();
                if (user != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception e)
            {
                return false;
            }
        }

    }
}
