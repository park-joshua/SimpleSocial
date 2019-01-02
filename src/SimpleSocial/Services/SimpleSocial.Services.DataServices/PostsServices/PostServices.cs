﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleSocia.Services.Models.Account;
using SimpleSocia.Services.Models.Comments;
using SimpleSocia.Services.Models.Followers;
using SimpleSocia.Services.Models.Posts;
using SimpleSocial.Data.Common;
using SimpleSocial.Data.Models;
using SimpleSocial.Services.Mapping;

namespace SimpleSocial.Services.DataServices.PostsServices
{
    public class PostServices : IPostServices
    {
        private readonly IRepository<Post> postRepository;
        private readonly IRepository<UserLike> userLikesRepository;
        private readonly IRepository<SimpleSocialUser> userRepository;
        private readonly IRepository<UserFollower> userFollowersRepository;
        private readonly UserManager<SimpleSocialUser> userManager;

        public PostServices(
            IRepository<Post> postRepository,
            IRepository<UserLike> userLikesRepository,
            IRepository<SimpleSocialUser> userRepository,
            IRepository<UserFollower> userFollowersRepository,
            UserManager<SimpleSocialUser> userManager
        )
        {
            this.postRepository = postRepository;
            this.userLikesRepository = userLikesRepository;
            this.userRepository = userRepository;
            this.userFollowersRepository = userFollowersRepository;
            this.userManager = userManager;
        }

        public void CreatePost(MyProfileViewModel viewModel)
        {
            var post = new Post
            {
                UserId = viewModel.CreatePost.UserId,
                Title = viewModel.CreatePost.Title,
                WallId = viewModel.CreatePost.WallId,
                Content = viewModel.CreatePost.Content,
            };


            postRepository.AddAsync(post).GetAwaiter().GetResult();
            postRepository.SaveChangesAsync().GetAwaiter().GetResult();
        }

        public int GetTotalPostsCount()
        {
            return this.postRepository.All().Count();
        }

        public ICollection<PostViewModel> GetUserPosts(string userId, string currrentUserId)
        {          
            var posts = this.postRepository.All().Include(p => p.User).ThenInclude(u => u.ProfilePicture).Include(p => p.Comments).ThenInclude(p => p.Author).ThenInclude(a => a.ProfilePicture).Select(x => Mapper.Map<PostViewModel>(x)).Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedOn).ToList();

            foreach (var post in posts)
            {
                var likes = userLikesRepository.All().Where(x => x.PostId == post.Id).ToList();
                if (likes.FirstOrDefault(x => x.UserId == currrentUserId) == null)
                {
                    post.IsLiked = false;
                }
                else
                {
                    post.IsLiked = true;
                }
                post.Likes = likes;
            }

            return posts;
        }


        public PostViewModel GetPostById(string id)
        {
            var post = this.postRepository.All().Include(p => p.User).ThenInclude(u => u.ProfilePicture).Include(x => x.Comments).ThenInclude(x => x.Author).ThenInclude(a => a.ProfilePicture).Select(x => Mapper.Map<PostViewModel>(x)).FirstOrDefault(x => x.Id == id);
            return post;
        }

        public SinglePostViewComponentModel GetSinglePostViewComponentModel(string id, string visitorId)
        {
            var userWithProfilePic = this.userRepository.All().Include(u => u.ProfilePicture).FirstOrDefault(x => x.Id == visitorId);
            var profilePicture = userWithProfilePic.ProfilePicture;
            var viewModel = new SinglePostViewComponentModel();
            var post = this.GetPostById(id);
            var likes = userLikesRepository.All().Where(x => x.PostId == post.Id).ToList();
            if (likes.FirstOrDefault(x => x.UserId == visitorId) == null)
            {
                post.IsLiked = false;
            }
            else
            {
                post.IsLiked = true;
            }
            post.Likes = likes;
            var postAuthorId = post.User.Id;
            if (post.Likes.FirstOrDefault(x => x.UserId == visitorId) == null)
            {
                post.IsLiked = false;
            }
            else
            {
                post.IsLiked = true;
            }
            viewModel.Post = post;
            viewModel.LikeClassName = "like-ajax-1";
            viewModel.ProfilePicture = profilePicture;
            viewModel.CommentInputModel = new CommentInputModel();
            viewModel.PostVisitorId = visitorId;
            viewModel.PostAuthorId = postAuthorId;
            return viewModel;
        }

        public bool UserCanSeePost(string id, ClaimsPrincipal user)
        {
            //TODO: change logic here
            var currentUser = this.userManager.GetUserAsync(user).GetAwaiter().GetResult();
            var post = this.postRepository.All().Include(x => x.User).FirstOrDefault(x => x.Id == id);

            var postAuthor = post.User;

            if (postAuthor.Id == currentUser.Id)
            {
                return true;
            }

            return true;

        }

        public ICollection<PostViewModel> GetNewsFeedPosts(string currrentUserId)
        {
            var posts = new List<PostViewModel>();
            var followings = this.userFollowersRepository.All().Where(x => x.FollowerId == currrentUserId);
            foreach (var user in followings)
            {
                var userPosts = this.postRepository.All().Include(x => x.Comments).ThenInclude(x => x.Author).ThenInclude(a => a.ProfilePicture).Include(x => x.User).ThenInclude(u => u.ProfilePicture).Where(x => x.UserId == user.UserId).Select(x => Mapper.Map<Post, PostViewModel>(x));
                foreach (var post in userPosts)
                {
                    posts.Add(post);
                }
            }

            posts = posts.OrderByDescending(x => x.CreatedOn).ToList();

            posts = CheckIsPostsAreLiked(currrentUserId, posts).ToList();

            return posts;
        }

        private ICollection<PostViewModel> CheckIsPostsAreLiked(string currrentUserId, List<PostViewModel> posts)
        {
            foreach (var post in posts)
            {
                var likes = userLikesRepository.All().Where(x => x.PostId == post.Id).ToList();
                if (likes.FirstOrDefault(x => x.UserId == currrentUserId) == null)
                {
                    post.IsLiked = false;
                }
                else
                {
                    post.IsLiked = true;
                }
                post.Likes = likes;
            }

            return posts;
        }
    }
}