﻿using ASOC.Domain;
using ASOC.WebUI.Infrastructure.Interfaces;
using ASOC.WebUI.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ASOC.WebUI.Controllers
{
    public class ModelController : Controller
    {
        IRepository<MODEL> modelRepository;
        IGetList getList;
        IRepository<TYPE> typeRepository;
        IRepository<PRICE> priceRepository;

        public ModelController(IRepository<MODEL> modelRepositoryParam, IGetList getListParam,
            IRepository<TYPE> typeRepositoryParam, IRepository<PRICE> priceRepositoryParam)
        {
            modelRepository = modelRepositoryParam;
            getList = getListParam;
            typeRepository = typeRepositoryParam;
            priceRepository = priceRepositoryParam;
        }

        // GET: Role
        public ActionResult Index(int? page, ModelViewModel modelData, string price)
        {            
                if (modelData.searchString != null)
                {
                    page = 1;
                }
                else
                {
                    modelData.ID_TYPE = modelData.ID_TYPE;
                }

                modelData.currentFilter = modelData.searchString;

                var models = modelRepository.GetAllList();               
                decimal searchDigit;
                bool isInt = Decimal.TryParse(modelData.searchString, out searchDigit);

                if (!String.IsNullOrEmpty(modelData.searchString))
                {
                    if (!isInt)
                    {
                        models = models.Where(s => s.NAME.Contains(modelData.searchString)).OrderBy(s => s.NAME);
                    }
                }

                if (modelData.ID_TYPE != 0)
                {
                    var type = typeRepository.GetAllList().First(m => m.ID.Equals(modelData.ID_TYPE));
                    models = models.Where(s => s.TYPE.NAME.Contains(type.NAME)).OrderBy(s => s.NAME);
                }                

                int pageSize = 10;
                int pageNumber = (page ?? 1);

                List<ModelViewModel> modelList = new List<ModelViewModel>();

                foreach (MODEL item in models)
                {
                    modelList.Add(new ModelViewModel()
                    {
                        COMPONENT = item.COMPONENT,
                        ID = item.ID,
                        ID_TYPE = item.ID_TYPE,
                        NAME = item.NAME,
                        PRICE = item.PRICE,
                        TYPE = item.TYPE,
                        currentCoast = item.PRICE.Where(x => x.ID_MODEL.Equals(item.ID))
                            .OrderByDescending(x => x.DATE_ADD).FirstOrDefault().COST
                    });
                }                           
               

               int min = Convert.ToInt32(modelList.Min(m => m.currentCoast));
               int max = Convert.ToInt32(modelList.Max(m => m.currentCoast));

            if (!String.IsNullOrEmpty(modelData.searchString))
                {
                    if (isInt)
                    {
                        modelList = modelList.FindAll(m => m.currentCoast.Equals(searchDigit));
                    }
                }
            //Проверка на слайдер 
            if (price != null && price != "")
            {
                String[] numbers = price.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                decimal num1 = Convert.ToDecimal(numbers[0]);
                decimal num2 = Convert.ToDecimal(numbers[1]);
                modelList = modelList.FindAll(m => m.currentCoast <= num2);
                modelList = modelList.FindAll(m => m.currentCoast >= num1);
            }



            ModelViewModel model = new ModelViewModel
                {
                    modelList = modelList.ToPagedList(pageNumber, pageSize),
                    typeList = getList.getTypeSelectList(),
                    searchString = modelData.searchString,
                    currentFilter = modelData.currentFilter,
                    ID_TYPE = modelData.ID_TYPE,
                    maxPrice = max,
                    minPrice = min 
                };
                return View(model);                    
        }

        public ActionResult ModelLog(int? id)
        {
            if (id != null)
                return RedirectToAction("Index", "Price", new { modelID = id });
            else
                return HttpNotFound();
        }

        public ActionResult Amount(int? id)
        {            
            if (id != null)
                return RedirectToAction("Index", "Component", new { modelID = id });
            else
                return HttpNotFound();
        }

        public ActionResult Details(int? page,int? id)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            if (id==null)
            {
                return HttpNotFound();
            }
            MODEL model = modelRepository.GetAllList().First(x => x.ID.Equals(Convert.ToDecimal(id)));

            ModelViewModel modelData = new ModelViewModel
            {
                ID = model.ID,
                ID_TYPE = model.ID_TYPE,
                NAME = model.NAME,
                TYPE = model.TYPE,
                currentCoast = model.PRICE.Where(x => x.ID_MODEL.Equals(model.ID))
                       .OrderByDescending(x => x.DATE_ADD).FirstOrDefault().COST,
                PRICE = model.PRICE,
                priceList = model.PRICE.ToPagedList(pageNumber, pageSize)
            };
            return View(modelData);
        }

        [HttpGet]
        public ActionResult PriceChange(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            MODEL model = modelRepository.GetAllList().
                FirstOrDefault(x => x.ID.Equals(Convert.ToDecimal(id)));

            if (model == null)
            {
                return HttpNotFound();
            }

            decimal coast = model.PRICE.Where(x => x.ID_MODEL.Equals(model.ID))
                       .OrderByDescending(x => x.DATE_ADD).FirstOrDefault().COST;

            ModelViewModel modelData = new ModelViewModel()
            {
                ID = model.ID,
                ID_TYPE = model.ID_TYPE,
                NAME = model.NAME,                
                currentCoast = coast
            };
            return View(modelData);          
        }

        [HttpPost]
        public ActionResult PriceChange(ModelViewModel modelData)
        {
            if (ModelState.IsValid)
            {
                PRICE price = new PRICE()
                {
                    COST = Convert.ToDecimal(modelData.currentCoast),
                    ID_MODEL = Convert.ToDecimal(modelData.ID),
                    DATE_ADD = DateTime.Now
                };
                priceRepository.Create(price);
                priceRepository.Save();
                return RedirectToAction("Index");
            }
            else
                return HttpNotFound();            
        }


        // GET: Delete
        public ActionResult Delete(int? id)
        {

            if (id == null)
            {
                return HttpNotFound();
            }

            MODEL status = modelRepository.GetAllList().First(x => x.ID.Equals(Convert.ToDecimal(id)));

            if (status == null)
            {
                return HttpNotFound();
            }

            return View(status);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {           
            modelRepository.Delete(id);
            modelRepository.Save();
            return RedirectToAction("Index");
        }

        // Get: Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            MODEL model = modelRepository.GetAllList().
                FirstOrDefault(x => x.ID.Equals(Convert.ToDecimal(id)));
           
            if (model == null)
            {
                return HttpNotFound();
            }

            decimal coast = model.PRICE.Where(x => x.ID_MODEL.Equals(model.ID))
                       .OrderByDescending(x => x.DATE_ADD).FirstOrDefault().COST;

            ModelViewModel modelData = new ModelViewModel()
            {
                ID = model.ID,
                ID_TYPE = model.ID_TYPE,
                NAME = model.NAME,
                typeList = getList.getTypeSelectList(),                
                currentCoast = coast,
                oldCoast = coast              
            };
            return View(modelData);
        }

        // POST: Edit              
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ModelViewModel modelData)
        {         
            if (ModelState.IsValid)
            {             
                MODEL model = new MODEL()
                    {
                        ID = modelData.ID,
                        ID_TYPE = modelData.ID_TYPE,
                        NAME = modelData.NAME
                    };
                modelRepository.Update(model);
                modelRepository.Save();

                if (modelData.currentCoast != modelData.oldCoast )
                {
                    PRICE price = new PRICE()
                    {
                        COST = Convert.ToDecimal(modelData.currentCoast),
                        ID_MODEL = Convert.ToDecimal(modelData.ID),
                        DATE_ADD = DateTime.Now
                    };
                    priceRepository.Create(price);
                    priceRepository.Save();
                }

                return RedirectToAction("Index");                
            }
            return View(modelData);
        }

        // Get: Create
        public ActionResult Create()
        {
            ModelViewModel modelData = new ModelViewModel()
            {               
                typeList = getList.getTypeSelectList()
            };
            return View(modelData);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelViewModel modelData)
        {
            if (ModelState.IsValid)
            {
                MODEL model = new MODEL()
                {
                    ID_TYPE = modelData.ID_TYPE,
                    NAME = modelData.NAME
                };
                IEnumerable<MODEL> sameModel = modelRepository.GetAllList().Where(x => x.NAME.Equals(modelData.NAME) 
                                                && x.ID_TYPE.Equals(modelData.ID_TYPE));
                if (sameModel != null)
                {
                    modelRepository.Create(model);
                    modelRepository.Save();
                }
                else
                    return HttpNotFound();

                MODEL modelFind = modelRepository.GetAllList().Where(x => x.NAME.Equals(modelData.NAME)
                                                && x.ID_TYPE.Equals(modelData.ID_TYPE)).First();
                PRICE price = new PRICE()
                {
                    COST = Convert.ToDecimal(modelData.currentCoast),
                    ID_MODEL = Convert.ToDecimal(modelFind.ID),
                    DATE_ADD = DateTime.Now
                };             
                priceRepository.Create(price);
                priceRepository.Save();

                return RedirectToAction("Index");
            }
            return View(modelData);
        }
    }
}