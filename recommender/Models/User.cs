using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using recommender.Services;

namespace recommender.Models
{
    public class User
    {
        /// <summary>
        /// user_id as string input from view home page
        /// </summary>
        public string user_id; // { get; set; }

        /// <summary>
        /// array of 10k elements represent ratings of 10k books by a user
        /// </summary>
        public int[] rating; // { get; set; }

        /// <summary>
        /// Book data from database connected via IBookService interface, will never change
        /// </summary>
        // private readonly IBookService _bookService;

        /// <summary>
        /// Rating data from database connected via IRatingService interface, can be changed by User and inherited to guest
        /// </summary>
        // protected IRatingService _ratingService;

        public User(){}

        /// <summary>
        /// constructor for User class
        /// </summary> 
        /* public User(IBookService bookService, IRatingService ratingService)
        {
            _bookService = bookService;
            _ratingService = ratingService;
        } */

        /// <summary>
        /// constructor for User object with known user_id (existing or new)
        /// </summary>
        /// <param name="user_id">user_id to be read during run-time</param>
        /// <returns>User object with pre-defined ratings and user_id as attributes</returns>
        // public User(IBookService bookService, IRatingService ratingService, string user_id) : this(bookService, ratingService)
        public User(string user_id) : this()
        {
            this.user_id = user_id;
        }
        
        /// <summary>
        /// Set ratings, different between existing user and guests
        /// </summary>
        public virtual void setRatings()
        {
            this.rating = Rating.getRatingByUser(this.user_id);
            /* int[][] user_jaggedarray = Rating.constructUserJaggedArray();
            try // if (Int32.Parse(user_id) >= 0 && Int32.Parse(user_id) < 52424)
            {                
                this.rating = user_jaggedarray[Convert.ToInt32(this.user_id)];
            }
            catch (IndexOutOfRangeException) // else
            {
                this.rating = user_jaggedarray[user_jaggedarray.Count()-1];
            } */
        }

        /// <summary>
        /// Set ratings for any user (new or existing)
        /// </summary>
        /// <param name="ratings">a 1D array of 10k elements of ratings from 0 to 5</param>
        public void setRatings(int[] ratings)
        {
            this.rating = ratings;
        }

        /// <summary>
        /// Set this User object's rating, the Book object's rating also changes
        /// </summary>
        /// <param name="book">the Book object to which the new rating is set</param>
        /// <param name="rating">new rating to be set</param>
        public void setRating(Book book, int rating)
        {
            this.rating[book.id-1] = rating;
            book.rating = rating;
        }

        /// <summary>
        /// Set this User object's rating, the Book object's rating remains unchanged
        /// </summary>
        /// <param name="col_index">column index in this.rating, equivalent to Book's id-1</param>
        /// <param name="rating">new rating to be set</param>
        public void setRating(int col_index, int rating)
        {
            this.rating[col_index] = rating;
        }

        /// <summary>
        /// Get a given user's ratings from existing data; method cannot be inherited
        /// </summary>
        /// <param name="user_jaggedarray">user(row)-book(column)-rating(value)</param>
        /// <param name="user_index">row_id representing the user in the jagged array</param>
        /// <returns>User class with this.rating extracted from existing data</returns>
        /* no longer in use, prototyping only
        private static User accessUser(int[][] user_jaggedarray, int user_index, IBookService bookService)
        {
            User current_user = new User(bookService);
            current_user.user_id = user_index.ToString();
            current_user.rating = user_jaggedarray[user_index];
            int no_book_rated = VectorOpt.CountNonZero(current_user.rating);
            // Console.WriteLine("The user rated {0} books.", no_book_rated);
            return current_user;
        } */ 

        /// <summary>
        /// Get books that a given user rated
        /// </summary>
        /// <returns>a list of rated books</returns>
        public List<Book> getRatedBook()
        {
            List<Book> rated_book = new List<Book>();

            for (int b = 0; b < 10000; b++) // replace this.rating.Length with 10000
            {
                if (this.rating[b] != 0)
                {
                    Book selected_book = new Book();
                    selected_book = Book.selectBook(b); // book without rating
                    
                    if (selected_book != null) // to prevent null reference
                    {
                        selected_book.rating = this.rating[b];
                        rated_book.Add(selected_book);
                    }                                       
                }
            }
            rated_book.Remove(null);
            return rated_book;
        }

        /// <summary>
        /// Get books recommended for a given user
        /// </summary>
        /// <returns>a list of recommended books</returns>
        public List<Book> getRecommendedBook() // combine similarUser() into this method
        {
            List<Book> recommended_book = new List<Book>(); 

            this.setRatings();
            if (this.rating.Sum() == 0) // redundancy to prevent ZeroDivisionError later
            {
                return recommended_book;
            }          
            
            int[][] user_jaggedarray = Rating.getJaggedArrayByUser(this.rating); 
            // int[][] user_jaggedarray = Rating.constructUserJaggedArray();         

            double similarity;
            int no_similar_users = 0;
            double[] sum_book_rating = new double[user_jaggedarray[0].Length];
            double[] cnt_book_rating = new double[user_jaggedarray[0].Length];
            double[] avg_book_rating = new double[user_jaggedarray[0].Length];
            int no_recommended_book = 0;
            
            Parallel.For(0, user_jaggedarray.Length, u =>
            {
                similarity = VectorOpt.cosineSimilarity(this.rating, user_jaggedarray[u]);
                if (similarity > 0.1) // minimum cosine similarity
                {
                    no_similar_users += 1;
                    for (int b = 0; b < 10000; b++)
                    {
                        // sum_book_rating[b] += accessUser(user_jaggedarray, u).rating[b];
                        sum_book_rating[b] += user_jaggedarray[u][b]*similarity;
                        if (user_jaggedarray[u][b] != 0) cnt_book_rating[b] += similarity; 
                    }
                }
            });
            // Console.WriteLine("There are {0} similar users.", no_similar_users-1);  
            for (int b = 0; b < user_jaggedarray[0].Length; b++)    
            {
                if (cnt_book_rating[b] > 0.8) // only include avg rating from book with high similarity
                {
                    // avg_book_rating[b] = sum_book_rating[b]/cnt_book_rating[b];
                    avg_book_rating[b] = sum_book_rating[b]; // consider weighted sum as avg for ranking
                }
            }              
            
            /* this algorithm doesn't rank the book, this is the area to be improved
            for (int b = 0; b < 10000; b++)
            {                
                if (sum_book_rating[b] > 5 && this.rating[b] == 0)
                {
                    try {
                        Book selected_book = Book.selectBook(b);
                        if (selected_book != null)
                        {
                            recommended_book.Add(selected_book);
                            no_recommended_book += 1;
                        }
                    }
                    catch {
                        continue;
                    }
                }
            }
            recommended_book.Remove(null); */

            // rank the book by similarity-weighted sum of book ratings
            double[] input = new double[user_jaggedarray[0].Length]; 
            avg_book_rating.CopyTo(input, 0);
            int[] indice = Enumerable.Range(0, input.Length).ToArray();
            Array.Sort(input, indice);
            Array.Reverse(indice);
            Array.Reverse(input);
            int index = 0;
            while (true)
            {   
                if (input[index] < 5) // stop when reach low rating, unlimited books
                {
                    break;
                }
                if (this.rating[indice[index]] == 0 && input[index] > 5)
                {
                    try {
                        Book selected_book = Book.selectBook(indice[index]);
                        if (selected_book != null)
                        {
                            recommended_book.Add(selected_book);
                            no_recommended_book += 1;
                        }
                    }
                    catch {
                        continue;
                    }
                }                
                index += 1;
            }
            // Console.WriteLine("{0} books are recommended.", recommended_book.Count());
            // foreach (Book book in recommended_book)
            // {
            //     Console.WriteLine(book.citeBook("unknown style"));
            // }
            return recommended_book;
        }
        public List<Book> rated_books;
        public List<Book> recommended_books;
        public List<Book> search_matched;
        public void setRatedBook()
        {
            this.rated_books = this.getRatedBook();
        }
        public void setRecommendedBook()
        {
            this.recommended_books = this.getRecommendedBook();
        }
        public void setSearchMatched(string text_input)
        {
            this.search_matched = Book.searchBook(text_input);
        }
    } 
}