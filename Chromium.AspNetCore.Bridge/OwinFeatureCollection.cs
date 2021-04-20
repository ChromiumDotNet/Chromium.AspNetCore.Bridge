// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;
using System.Collections.Generic;

namespace Chromium.AspNetCore.Bridge
{
    public class OwinFeatureCollection : FeatureCollection
    {
        public OwinFeatureCollection(IDictionary<string, object> environment) : base()
        {
            var owin = new OwinFeatureImpl(environment);

            Set<IHttpRequestFeature>(owin);
            Set<IHttpResponseFeature> (owin);
            Set<IHttpResponseBodyFeature> (owin);
            Set<IHttpRequestIdentifierFeature> (owin);
            Set<IOwinEnvironmentFeature>(owin);
        }
    }
}
