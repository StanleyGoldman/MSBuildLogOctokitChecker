﻿using System.Linq;
using MSBLOC.Core.Model;
using MSBLOC.Infrastructure.Models;

namespace MSBLOC.Web.ViewModels
{
    public class ListRepositoriesViewModel: ViewModelBase
    {
        public IGrouping<string, UserRepository>[] RepositoriesByOwner { get; set; }
        public ILookup<long, AccessToken> TokenLookup { get; set; }
        public string CreatedToken { get; set; }
        public long? CreatedTokenRepoId { get; set; }
    }
}