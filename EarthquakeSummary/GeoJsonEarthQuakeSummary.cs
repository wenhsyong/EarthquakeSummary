using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthquakeAlert
{
    class EarthSummaryJson
    {
    }
}

public class EarthQuakeSummary
{
    public string type { get; set; }
    public Metadata metadata { get; set; }
    public Feature[] features { get; set; }
    public float[] bbox { get; set; }
}

public class Metadata
{
    public long generated { get; set; }
    public string url { get; set; }
    public string title { get; set; }
    public int status { get; set; }
    public string api { get; set; }
    public int count { get; set; }
}

public class Feature
{
    public string type { get; set; }
    public Properties properties { get; set; }
    public Geometry geometry { get; set; }
    public string id { get; set; }
}

public class Properties
{
    public float mag { get; set; }
    public string place { get; set; }
    public long time { get; set; }
    public long updated { get; set; }
    public object tz { get; set; }
    public string url { get; set; }
    public string detail { get; set; }
    public object felt { get; set; }
    public object cdi { get; set; }
    public object mmi { get; set; }
    public object alert { get; set; }
    public string status { get; set; }
    public int tsunami { get; set; }
    public int sig { get; set; }
    public string net { get; set; }
    public string code { get; set; }
    public string ids { get; set; }
    public string sources { get; set; }
    public string types { get; set; }
    public object nst { get; set; }
    public object dmin { get; set; }
    public object rms { get; set; }
    public object gap { get; set; }
    public string magType { get; set; }
    public string type { get; set; }
    public string title { get; set; }
}

public class Geometry
{
    public string type { get; set; }
    public float[] coordinates { get; set; }
}
