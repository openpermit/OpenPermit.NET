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

using GeoJSON.Net.Feature;
using System.Reflection;

namespace OpenPermit
{
    /// <summary>
    /// Web API for the OpenPermit REST Resources
    /// JSON in and out follows BLDS format.
    /// </summary>
    [RoutePrefix("op/permits")]
    [UnhandledExceptionFilter]
    public class OpenPermitController : ApiController
    {
        public IOpenPermitAdapter Adapter { get; set; }

        private FeatureCollection ToGeoJson(List<Permit> permits)
        {
            var features = new List<Feature>(permits.Count);

            foreach(var permit in permits)
            {
                var point = new GeoJSON.Net.Geometry.Point(new GeoJSON.Net.Geometry.GeographicPosition(permit.Latitude, permit.Longitude));
                var properties = new Dictionary<string, object>();
                foreach(var property in permit.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var value = property.GetValue(permit, null); 
                    if(value != null)
                    {
                        properties.Add(property.Name, value);
                    }
                }
                features.Add(new Feature(point, properties, permit.PermitNum));
            }

            return new FeatureCollection(features);
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
        public HttpResponseMessage GetPermits(string number = null, string address = null, string bbox = null)
        {
            var filter = new PermitFilter
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
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                double xMin, yMin, xMax, yMax;
                if(!double.TryParse(coordinates[0], out xMin))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                if (!double.TryParse(coordinates[1], out yMin))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                if (!double.TryParse(coordinates[2], out xMax))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                if (!double.TryParse(coordinates[3], out yMax))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                filter.BoundingBox = new Box
                {
                    MinX = xMin,
                    MinY = yMin,
                    MaxX = xMax,
                    MaxY = yMax
                };
            }
            List<Permit> permits = Adapter.SearchPermits(filter);

            if (permits != null)
            {
                // TODO perhaps do this GeoJSON support as a filter?
                IEnumerable<string> acceptHeader;
                if(Request.Headers.TryGetValues("Accept", out acceptHeader) && acceptHeader.FirstOrDefault() == "application/vnd.geo+json")
                {
                    return Request.CreateResponse<FeatureCollection>(ToGeoJson(permits));
                }

                return Request.CreateResponse<List<Permit>>(permits);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
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
