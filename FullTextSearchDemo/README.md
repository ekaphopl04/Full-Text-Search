# Full-Text Search Demo with PostgreSQL and EF Core

This project demonstrates how to implement full-text search functionality in a .NET application using PostgreSQL and Entity Framework Core.

## Features

- Full-text search using PostgreSQL's built-in text search capabilities
- Entity Framework Core integration
- Automatic search vector updates via PostgreSQL triggers
- Sample product data for testing

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL database server
- Basic knowledge of C# and Entity Framework Core

## Database Setup

The application is configured to connect to a PostgreSQL database with the following connection string:
```
Host=localhost;Database=fulltext_search_demo;Username=postgres;Password=postgres
```

You may need to modify this connection string in `Program.cs` to match your PostgreSQL setup.

## How It Works

1. The application creates a `Product` model with properties like Name, Description, Category, and Price
2. A special `SearchVector` property stores the tsvector representation for full-text search
3. PostgreSQL triggers automatically update the search vector when products are added or modified
4. A custom SQL function `search_products` performs the actual full-text search using tsquery
5. Entity Framework Core maps this function to a C# method for easy use in code

## Running the Application

1. Ensure PostgreSQL is running
2. Update the connection string if needed
3. Run the application
4. Enter search terms when prompted
5. View the search results

## Search Tips

- Multiple words are combined with AND logic
- Try searching for terms like "laptop", "smartphone", or "apple"
- The search includes product names, descriptions, and categories

## Implementation Details

- Uses GIN index for efficient full-text search
- Implements ranking to show most relevant results first
- Automatically seeds the database with sample products if empty
