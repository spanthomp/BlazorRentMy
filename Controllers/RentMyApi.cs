using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMyApi.Data;
using RentMyApi.Models;
using System.Threading.Tasks;

namespace RentMyApi.Controllers
{
    [Route("api/[controller]")] //api/namespace of class i.e rentmyapi
    [ApiController] //tells asp.net that this controller will be an api
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    //within the authorization reference you need to tell it which schema it needs to follow

    public class RentMyApiController : ControllerBase
        //using controllerbase instead of just controller as were using rest api not mvc
    {
        //connect controller to apidbcontext
        //create a new instance to api dbcontext
        private readonly ApiDbContext _context;

        //then define contructor and inject apidbcontext to contructor
        public RentMyApiController(ApiDbContext context)
            {
            //then link them together inside constructor
            //initialised using dependancy injection - this is done by looking at service we added in 
            //configureservices method in startup file
            _context = context;
        }

    [HttpGet]
        //start with a get attribute
        //make a method to return all items
        public async Task<IActionResult> GetItems()
        {
            //connect to database and return items 
            //_context is the dbcotext and Items is the items table created in it
            var items = await _context.Items.ToListAsync(); //need to add await because were using aysnc, and because were using await we use async
            //because were reading data from somewhere outside system, you should use async to not hold the main thread of app whilst its running

            //once all items are back need to send bakc to user/client by using ok keyword
            return Ok(items);
            //putting items inside ok means youre returning status:200 as well as returning in json format
        }

        [HttpPost]
        //when communicating with database use async and put in a task
        //for us to process need to send it with json which has the format of ItemData and data
        public async Task<IActionResult> CreateItem(ItemData data)
        {
            //asp.net provides model state validation
            if (ModelState.IsValid)
            {
                //use await when communicating with database - this adds to memory within the server itself
                await _context.Items.AddAsync(data);
                //need to save changes to database - this takes changes done and generates a sql for then and executes to database
                await _context.SaveChangesAsync();
                //rest api standards mean we have to return object back with status code 
                //created at action is built in method that returns status code. you then show item created by id and list data
                return CreatedAtAction("GetItem", new { data.Id }, data);

            }

            return new JsonResult("Something went wrong") { StatusCode = 500};
        }

        [HttpGet("{id}")] //need to utilise item id thats why it is used in httpget parameter
        //this is used to get singular item used in create item function
        public async Task<IActionResult> GetItem(int id)
        {
            //connect to application dbcontext and get singular item
            //first or default is another ef keyword
            //then need to pass the condition which will match by id
            var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

            //then need to double check item is ot empty
            if(item == null) 
                return NotFound();
            //if okay returns with status code 200 (this is built in to ok keyword
            return Ok(item);
        }

        //put instead of post because it puts data in place of the already posted 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(int id, ItemData item) //pass id and itemdata is object thats updated
        {
            //first check the id sending matches the id within the item data
            if(id != item.Id)
                return BadRequest(); //this is rest standard

            //oce we know both ids are equal need to check it actually exists in database
            //create new variable called exist
            //again first or default async is built in method and you match it on the id (condition)
            var existItem = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

            //if exists equals null return not found
            if (existItem == null)
                return NotFound();

            //once we know there is an item with that id and we need to update it
            existItem.Name = item.Name;
            existItem.Description = item.Description;
            existItem.Price = item.Price;
            existItem.Available = item.Available;
            //basically taking all the values that comes from item data and save inside exist item
            // a better way would be to use automapper rather than putting all values in one by one

            //again, once saved need to update database context, as mentioned before this is saved in memory in server
            await _context.SaveChangesAsync();

            //in order to follow standard for rest need to send response
            return NoContent(); 
            //built in function, will return 204 which means unsuccessful
        }
       
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var existItem = await _context.Items.FirstOrDefaultAsync(x => x.Id ==id);

            if (existItem == null)
                return NotFound();

            _context.Items.Remove(existItem);
            await _context.SaveChangesAsync();

            //rest api standard again, return object
            return Ok(existItem);

        }
    }
}
