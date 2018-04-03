﻿// Copyright (c) 2018 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT licence. See License.txt in the project root for license information.


using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace GenericServices
{
    /// <summary>
    /// This is the sync interface to GenericService, which assumes you have one DbContext which the GenericService setup code will register to the DbContext type
    /// You should use this with dependency injection to get an instance of the sync GenericService
    /// </summary>
    public interface IGenericService : IStatusGeneric
    {
        /// <summary>
        /// This allows you to access the current DbContext that this instance of the GenericService is using.
        /// That is useful if you need to set up some properties in the DTO that cannot be found in the Entity
        /// For instance, setting up a dropdownlist based on some other database data
        /// </summary>
        DbContext CurrentContext { get; }

        /// <summary>
        /// This reads a single entity or DTO given the key(s) of the entity you want to load
        /// </summary>
        /// <typeparam name="T">This should either be an entity class or a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</typeparam>
        /// <param name="keys">The key(s) value. If there are multiple keys they must be in the correct order as defined by EF Core</param>
        /// <returns>The single entity or DTO that was found by the keys. If its an entity class then it is tracked</returns>
        T ReadSingle<T>(params object[] keys) where T : class;

        /// <summary>
        /// This reads a single entity or DTO using a where clause
        /// </summary>
        /// <typeparam name="T">This should either be an entity class or a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</typeparam>
        /// <param name="whereExpression">The where expression should return a single instance, otherwise you get a </param>
        /// <returns>The single entity or DTO that was found by the where clause. If its an entity class then it is tracked</returns>
        T ReadSingle<T>(Expression<Func<T, bool>> whereExpression) where T : class;

        /// <summary>
        /// This projects an entity to DTO and selects one using the primary keys 
        /// It then and shallow copies the result into an existing DTO. Useful for Razor Pages
        /// </summary>
        /// <typeparam name="TDto">This type is found from the input dto</typeparam>
        /// <param name="dto">This must be a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</param>
        /// <param name="keys">Optional: If you provide keys then the method will use them, otherwise it will extract the keys
        /// from the dto's properties.</param>
        void ReadSingleToDto<TDto>(TDto dto, params object[] keys) where TDto : class;

        /// <summary>
        /// This projects an entity to DTO and selects one using the where clause. 
        /// It then and shallow copies the result into an existing DTO. Useful for Razor Pages.
        /// </summary>
        /// <typeparam name="TDto">This type is found from the input dto</typeparam>
        /// <param name="dto">This must be a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</param>
        /// <param name="whereExpression">The where expression should return a single instance, otherwise you get a </param>
        /// <returns></returns>
        void ReadSingleToDto<TDto>(TDto dto, Expression<Func<TDto, bool>> whereExpression) where TDto : class;

        /// <summary>
        /// This returns an <see cref="IQueryable{T}"/> result, where T can be either an actual entity class,
        /// or if a GenericService DTO is provided then the linked entity class will be projected via AutoMapper to the DTO
        /// </summary>
        /// <typeparam name="T">This should either be an entity class or a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</typeparam>
        /// <returns>an <see cref="IQueryable{T}"/> result. You should apply a execute method, e.g. .ToList() or .ToListAsync() to execute the result</returns>
        IQueryable<T> ReadManyNoTracked<T>() where T : class;

        /// <summary>
        /// This will create a new entity in the database. If you provide class which is an entity class (i.e. in your EF Core database) then
        /// the method will add, and then call SaveChanges. If the class you provide is a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface
        /// it will use that to create the entity by matching the DTOs properties to either, a) a public static method, b) a public ctor, or 
        /// c) by setting the properties with public setters in the entity (using AutoMapper)
        /// </summary>
        /// <typeparam name="T">This type is found from the input instance</typeparam>
        /// <param name="entityOrDto">This should either be an instance of a entity class or a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</param>
        /// <param name="ctorOrStaticMethodName">Optional: you can tell GenericServices which static method, ctor or "AutoMapper" to use</param>
        /// <returns></returns>
        T AddNewAndSave<T>(T entityOrDto, string ctorOrStaticMethodName = null) where T : class;

        /// <summary>
        /// This will update the entity referred to by the keys in the given class instance.
        /// For a entity class instance it will check the state of the instance. If its detached it will call Update, otherwise it assumes its tracked and calls SaveChanges
        /// For a GenericService DTO it will: 
        /// a) load the existing entity class using the primary key(s) in the DTO
        /// b) This it will look for a public method that match the DTO's properties to do the update, or if no method is found it will try to use AutoMapper to copy the data over to the e
        /// c) finally it will call SaveChanges
        /// </summary>
        /// <typeparam name="T">This type is found from the input instance</typeparam>
        /// <param name="entityOrDto">This should either be an instance of a entity class or a GenericService DTO which has a <see cref="ILinkToEntity{TEntity}"/> interface</param>
        /// <param name="methodName">Optional: you can give the method name to be used for the update, or "AutoMapper" to make it use AutoMapper to update the entity.</param>
        void UpdateAndSave<T>(T entityOrDto, string methodName = null) where T : class;

        /// <summary>
        /// This will delete the entity class with the given primary key
        /// </summary>
        /// <typeparam name="TEntity">The entity class you want to delete. It should be an entity in the DbContext you are referring to.</typeparam>
        /// <param name="keys"></param>
        void DeleteAndSave<TEntity>(params object[] keys) where TEntity : class;

        /// <summary>
        /// This will find entity class with the given primary key, then call the method you provide before calling the Remove method + SaveChanges.
        /// Your method has access to the database and can handle any relationships, and returns an <see cref="IStatusGeneric"/>. The Remove will 
        /// only go ahead if the status your method returns is Valid, i.e. no errors
        /// </summary>
        /// <typeparam name="TEntity">The entity class you want to delete. It should be an entity in the DbContext you are referring to.</typeparam>
        /// <param name="runBeforeDelete"></param>
        /// <param name="keys"></param>
        void DeleteWithActionAndSave<TEntity>(Func<DbContext, TEntity, IStatusGeneric> runBeforeDelete,
            params object[] keys) where TEntity : class;
    }
}