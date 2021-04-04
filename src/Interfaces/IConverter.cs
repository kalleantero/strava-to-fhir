using System;
using System.Collections.Generic;
using System.Text;

namespace StravaActivityToFhir.Interfaces
{
public interface IConverter<TSource, TDestination>
{
    TDestination Convert(TSource source_object);
}
}
