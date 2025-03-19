using AutoMapper;
using System;
using WebBiaProject.Data;
using WebBiaProject.Models;
using WebBiaProject.Repositories.IRepository;

namespace WebBiaProject.Repositories.Repository
{
    public class BilliardTableRepository: RepositoryBase<BilliardTable>, IBilliardTableRepository
    {
        public BilliardTableRepository(ApplicationDbContext context) : base(context)
        {
        }

    }
}
