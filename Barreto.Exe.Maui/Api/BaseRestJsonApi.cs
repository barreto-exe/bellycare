﻿using Barreto.Exe.Maui.Services.Navigation;
using Barreto.Exe.Maui.Services.Settings;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Barreto.Exe.Maui.Api
{
    public abstract class BaseRestJsonApi
    {
        protected HttpClient Client { get; private set; }
        readonly JsonSerializerSettings serializerSettings;
        readonly IBaseSettingsService settingsService;
        readonly INavigationService navigationService;


        public BaseRestJsonApi(IBaseSettingsService settingsService, INavigationService navigationService)
        {
            Client = new HttpClient();
            serializerSettings = new()
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"),
            };
            this.settingsService = settingsService;
            this.navigationService = navigationService;

            RefreshToken();
        }

        protected void RefreshToken()
        {
            var token = settingsService.AccessToken;
            if (!string.IsNullOrEmpty(token))
            {
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        protected async Task<ApiResponse<T>> GetAsync<T>(string url, object query = null)
        {
            url = AppendQueryFilters(url, query);

            try
            {
                RefreshToken();
                var response = await Client.GetAsync(url);
                return await GeneralResponse<T>(response);
            }
            catch (Exception ex)
            {
                await ToastNoInternetMessage();
                return default;
            }
        }
        protected async Task<object> GeneralGetAsync(string url, object query = null)
        {
            url = AppendQueryFilters(url, query);
            try
            {
                var response = await Client.GetAsync(url);
                string responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(responseString, serializerSettings);
            }
            catch
            {
                await ToastNoInternetMessage();
                return default;
            }
        }
        private static string AppendQueryFilters(string url, object query)
        {
            //Add query parameters to url with object reflection
            if (query != null)
            {
                var properties = query.GetType().GetProperties();
                var queryString = new StringBuilder();
                queryString.Append('?');
                foreach (var property in properties)
                {
                    if (property != null && property.GetValue(query) != null)
                    {
                        queryString.Append($"{property.Name.ToLower()}={property.GetValue(query)}&");
                    }
                }
                url += queryString.ToString().TrimEnd('&');
            }

            return url;
        }

        protected async Task<ApiResponse<T>> PostAsync<T>(string url, object data)
        {
            StringContent content = null;
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data, serializerSettings);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                RefreshToken();
                var response = await Client.PostAsync(new Uri(url), content);
                return await GeneralResponse<T>(response);
            }
            catch(Exception ex)
            {
                await ToastNoInternetMessage();
                return default;
            }
        }
        protected async Task<ApiResponse<T>> PutAsync<T>(string url, object data = null)
        {
            StringContent content = null;
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data, serializerSettings);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                RefreshToken();
                var response = await Client.PutAsync(new Uri(url), content);
                return await GeneralResponse<T>(response);
            }
            catch
            {
                await ToastNoInternetMessage();
                return default;
            }
        }
        protected async Task<ApiResponse<T>> DeleteAsync<T>(string url)
        {
            try
            {
                RefreshToken();
                var response = await Client.DeleteAsync(new Uri(url));
                return await GeneralResponse<T>(response);
            }
            catch
            {
                await ToastNoInternetMessage();
                return default;
            }
        }
        private async Task<ApiResponse<T>> GeneralResponse<T>(HttpResponseMessage response)
        {
            string responseString = await response.Content.ReadAsStringAsync();

            if (responseString.Contains("INVALID_INPUT"))
            {
                await ToastInvalidInput();
                return new ApiResponse<T>()
                {
                    Data = default,
                    Message = "INVALID_INPUT",
                };
            }
            else if (!response.IsSuccessStatusCode)
            {
                await ToastMessage(response.StatusCode);
            }


            return JsonConvert.DeserializeObject<ApiResponse<T>>(responseString, serializerSettings);
        }

        private async Task ToastNoInternetMessage()
        {
            var page = Application.Current.MainPage;
            await page.DisplayAlert("Error", "Parece que no tienes conexión a internet.", "Aceptar");

            settingsService.AccessToken = string.Empty;
            navigationService.RestartSession();
        }
        private async Task ToastInvalidInput()
        {
            var page = Application.Current.MainPage;
            await page.DisplayAlert("Error", "Los datos ingresados no son válidos", "Aceptar");
        }
        private async Task ToastMessage(HttpStatusCode code)
        {
            var page = Application.Current.MainPage;

            switch (code)
            {
                case HttpStatusCode.BadRequest:
                    break;

                case HttpStatusCode.Unauthorized:
                    await page.DisplayAlert(
                        "Error",
                        "Tu sesión ha caducado",
                        "Aceptar");

                    settingsService.AccessToken = string.Empty;
                    navigationService.RestartSession();
                    break;

                case HttpStatusCode.Forbidden:
                    await page.DisplayAlert(
                        "Error",
                        "No tienes permiso para realizar esta acción",
                        "Aceptar");
                    break;

                case HttpStatusCode.NotFound:
                    await page.DisplayAlert(
                        "Error",
                        "No se encontró el recurso solicitado",
                        "Aceptar");
                    break;

                case HttpStatusCode.InternalServerError:
                    await page.DisplayAlert(
                        "Error",
                        "Ha ocurrido un error en el servidor. No es tu culpa, intentaremos solucionarlo en el menor tiempo posible.",
                        "Aceptar");

                    break;

                case
                HttpStatusCode.ServiceUnavailable:
                    await page.DisplayAlert(
                        "Error",
                        "El servidor no está disponible en este momento. Intenta más tarde.",
                        "Aceptar");
                    break;

                default:
                    await page.DisplayAlert(
                        "Error",
                        "Ha ocurrido un error inesperado. No es tu culpa, intentaremos solucionarlo en el menor tiempo posible.",
                        "Aceptar");
                    break;
            }
        }
    }
}
