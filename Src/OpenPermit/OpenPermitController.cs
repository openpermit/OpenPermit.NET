using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Collections.Specialized;
using System.Web;
using System.Configuration;
using System.Net.Http.Headers;
using System.Web.Http.Cors;

using Newtonsoft.Json;


namespace OpenPermit
{
    /// <summary>
    /// Web API for the OpenPermit REST Resources.
    /// JSON in and out follows BLDS format.
    /// </summary>
    [RoutePrefix("op/permits")]
    [UnhandledExceptionFilter]
    public class OpenPermitController : ApiController
    {
        public IOpenPermitAdapter Adapter { get; set; }

        private PushStreamContent ToGeoJsonStream(List<Permit> permits, FieldChoices choice)
        {
            PushStreamContent geoJsonContent = new PushStreamContent(
                (stream, content, context) =>
                {
                    TextWriter writer = new StreamWriter(stream);
                    JsonWriter jsonWriter = new JsonTextWriter(writer);
                    jsonWriter.Formatting = Formatting.None;
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("type");
                    jsonWriter.WriteValue("FeatureCollection");
                    jsonWriter.WritePropertyName("features");
                    jsonWriter.WriteStartArray();
                    
                    foreach (var permit in permits)
                    {
                        jsonWriter.WriteStartObject();
                        jsonWriter.WritePropertyName("type");
                        jsonWriter.WriteValue("Feature");
                        jsonWriter.WritePropertyName("id");
                        jsonWriter.WriteValue(permit.PermitNum);
                        jsonWriter.WritePropertyName("geometry");
                        jsonWriter.WriteStartObject();
                        jsonWriter.WritePropertyName("type");
                        jsonWriter.WriteValue("Point");
                        jsonWriter.WritePropertyName("coordinates");
                        jsonWriter.WriteStartArray();
                        jsonWriter.WriteValue(permit.Longitude);
                        jsonWriter.WriteValue(permit.Latitude);
                        jsonWriter.WriteEndArray();
                        jsonWriter.WriteEndObject();

                        if((choice & FieldChoices.Recommended) > 0)
                        {
                            // TODO Serialize required fields
                        }

                        if ((choice & FieldChoices.Optional) > 0)
                        {
                            // TODO Serialize optional fields
                        }

                        if ((choice & FieldChoices.All) > 0)
                        {
                            // TODO Serialize all
                        }

                        jsonWriter.WriteEndObject();
                    }
                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                    jsonWriter.Flush();
                    jsonWriter.Close();
                });

            return geoJsonContent;
        }

        private bool TryPopulatePermitFilter(out PermitFilter filter,
                                             string number, 
                                             string address, 
                                             string bbox,
                                             string types,
                                             string fields)
        {
            filter = new PermitFilter
            {
                PermitNumber = number,
                Address = address
            };

            // Bounding box search follows OpenSearch Geo Extensions (see: http://www.opensearch.org/ look under Geo extensions)
            if(bbox != null)
            {
                string[] coordinates = bbox.Split(',');
                if(coordinates.Length != 4)
                {
                    return false;
                }

                double xMin, yMin, xMax, yMax;
                if(!double.TryParse(coordinates[0], out xMin))
                {
                    return false;
                }
                if (!double.TryParse(coordinates[1], out yMin))
                {
                    return false;
                }
                if (!double.TryParse(coordinates[2], out xMax))
                {
                    return false;
                }
                if (!double.TryParse(coordinates[3], out yMax))
                {
                    return false;
                }

                filter.BoundingBox = new Box
                {
                    MinX = xMin,
                    MinY = yMin,
                    MaxX = xMax,
                    MaxY = yMax
                };
            }

            switch(fields.ToLower())
            {
                case "geo":
                    filter.Fields = FieldChoices.Geo;
                    break;
                case "required":
                    filter.Fields = FieldChoices.Recommended;
                    break;
                case "optional":
                    filter.Fields = FieldChoices.Optional;
                    break;
                default:
                    filter.Fields = FieldChoices.All;
                    break;
            }

            if(types != null)
            {
                string[] typesArray = types.Split(',');
                var choices = new List<TypeChoices>();
                foreach(string type in typesArray)
                {
                    TypeChoices choice;
                    if(Enum.TryParse<TypeChoices>(type, true, out choice))
                    {
                        choices.Add(choice);
                    }
                    
                    //TODO Ignoring bad inputs, should we return an error?
                }
                
                if(choices.Count > 0)
                {
                    filter.Types = choices;
                }
            }

            return true;
        }

        /// <summary>
        /// Implements OpenPermit Specification "GET permits" endpoint
        /// </summary>
        /// <param name="number">Permit number</param>
        /// <param name="address">Address of the location where to retrieve permits</param>
        /// <param name="bbox">
        ///     Replaced with the bounding box to search for geospatial results within. The box is defined by "west, south, east, north" coordinates
        ///     of longitude, latitude, in a EPSG:4326 decimal degrees. This is also commonly referred to by minX, minY, maxX, maxY (where longitude
        ///     is the X-axis, and latitude is the Y-axis), or also SouthWest corner and NorthEast corner.
        /// </param>
        /// <returns>
        ///     List of permits in BLDS format.
        ///     
        ///     Note: This endpoint GeoJSON reponses.
        /// </returns>
        [Route]
        public HttpResponseMessage GetPermits(string number = null, 
                                              string address = null, 
                                              string bbox = null,
                                              string types = null,
                                              string fields = "all")
        {

            var filter = new PermitFilter();
            if (TryPopulatePermitFilter(out filter, number, address, bbox, types, fields))
            {
                List<Permit> permits = Adapter.SearchPermits(filter);

                if (permits != null)
                {
                    // TODO perhaps do this GeoJSON support as a filter?
                    IEnumerable<string> acceptHeader;
                    if (Request.Headers.TryGetValues("Accept", out acceptHeader) && acceptHeader.FirstOrDefault() == "application/vnd.geo+json")
                    {
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        response.Content = ToGeoJsonStream(permits, filter.Fields);
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.geo+json");
                        return response;
                    }

                    return Request.CreateResponse<List<Permit>>(permits);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [Route("{number}")]
        public HttpResponseMessage GetPermit(string number, string options = null)
        {
            Permit permit = Adapter.GetPermit(number);

            if (permit.PermitNum != null)
            {
                return Request.CreateResponse<Permit>(permit);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Route("{number}/timeline")]
        public HttpResponseMessage GetPermitTimeline(string number, string options = null)
        {
            List<PermitStatus> timeline = Adapter.GetPermitTimeline(number);

            if (timeline != null)
            {
                return Request.CreateResponse<List<PermitStatus>>(timeline);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Route("{number}/inspections")]
        public HttpResponseMessage GetInspections(string number, string options = null)
        {
            List<Inspection> inspections = Adapter.GetInspections(number);

            if (inspections != null)
            {
                return Request.CreateResponse<List<Inspection>>(inspections);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Route("{number}/inspections/{inspectionId}")]
        public HttpResponseMessage GetInspection(string number, string inspectionId, string options = null)
        {
            Inspection inspection = Adapter.GetInspection(number, inspectionId);

            if (inspection != null)
            {
                return Request.CreateResponse<Inspection>(inspection);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Route("{number}/contractors")]
        public HttpResponseMessage GetContractors(string number, string options = null)
        {
            List<Contractor> contractors = Adapter.GetContractors(number);

            if (contractors != null)
            {
                return Request.CreateResponse<List<Contractor>>(contractors);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Route("{number}/contractors/{contractorId}")]
        public HttpResponseMessage GetContractor(string number, string contractorId, string options = null)
        {
            Contractor contractor = Adapter.GetContractor(number, contractorId);

            if (contractor != null)
            {
                return Request.CreateResponse<Contractor>(contractor);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }
    }
}
