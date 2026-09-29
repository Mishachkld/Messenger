CREATE TABLE "user" (
    id integer PRIMARY KEY,
    name character(128) NOT NULL,
    display_name character(128) NOT NULL,
    password character(255) NOT NULL
);