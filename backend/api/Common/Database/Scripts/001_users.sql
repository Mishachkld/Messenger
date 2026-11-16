CREATE TABLE "users"
(
    id    integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  character(128) NOT NULL,
    login character(128) NOT NULL
);