using Amazon;

namespace MiddleWare.DataStoreConnector.Amazon
{
    /// <summary>
    /// Amazon SDK Helper
    /// </summary>
    public static class AwsHelper
    {
        /// <summary>
        /// string을 AWS RegionEndpoint 로 타입 변경
        /// </summary>
        /// <param name="regionstring"></param>
        /// <returns></returns>
        public static RegionEndpoint? GetRegionEndpoint(string? regionstring)
        {
            if (regionstring == null) 
                return null;

            RegionEndpoint? region = null!;
            switch (regionstring)
            {
                case "AFSouth1": region = RegionEndpoint.AFSouth1; break;
                case "APEast1": region = RegionEndpoint.APEast1; break;
                case "APNortheast1": region = RegionEndpoint.APNortheast1; break;
                case "APNortheast2": region = RegionEndpoint.APNortheast2; break;
                case "APNortheast3": region = RegionEndpoint.APNortheast3; break;
                case "APSouth1": region = RegionEndpoint.APSouth1; break;
                case "APSoutheast1": region = RegionEndpoint.APSoutheast1; break;
                case "APSoutheast2": region = RegionEndpoint.APSoutheast2; break;
                case "APSoutheast3": region = RegionEndpoint.APSoutheast3; break;
                case "CACentral1": region = RegionEndpoint.CACentral1; break;
                case "CNNorth1": region = RegionEndpoint.CNNorth1; break;
                case "CNNorthWest1": region = RegionEndpoint.CNNorthWest1; break;
                case "EUCentral1": region = RegionEndpoint.EUCentral1; break;
                case "EUNorth1": region = RegionEndpoint.EUNorth1; break;
                case "EUSouth1": region = RegionEndpoint.EUSouth1; break;
                case "EUWest1": region = RegionEndpoint.EUWest1; break;
                case "EUWest2": region = RegionEndpoint.EUWest2; break;
                case "EUWest3": region = RegionEndpoint.EUWest3; break;
                case "MESouth1": region = RegionEndpoint.MESouth1; break;
                case "SAEast1": region = RegionEndpoint.SAEast1; break;
                case "USEast1": region = RegionEndpoint.USEast1; break;
                case "USEast2": region = RegionEndpoint.USEast2; break;
                case "USGovCloudEast1": region = RegionEndpoint.USGovCloudEast1; break;
                case "USGovCloudWest1": region = RegionEndpoint.USGovCloudWest1; break;
                case "USIsobEast1": region = RegionEndpoint.USIsobEast1; break;
                case "USIsoEast1": region = RegionEndpoint.USIsoEast1; break;
                case "USIsoWest1": region = RegionEndpoint.USIsoWest1; break;
                case "USWest1": region = RegionEndpoint.USWest1; break;
                case "USWest2": region = RegionEndpoint.USWest2; break;
                default: break;
            }

            return region;
        }
    }
}
