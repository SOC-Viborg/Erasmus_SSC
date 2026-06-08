using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Erasmus_SSC.Tests.Api;

    public class NewsApiTests
    {
        [Fact]
        public async Task GetNews_ShouldReturnSuccess()
    {
        await using var app = new WebApplicationFactory<Erasmus_SSC.Program>();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/news");

        response.EnsureSuccessStatusCode();
    }
    }

