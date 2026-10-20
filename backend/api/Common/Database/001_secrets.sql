CREATE TABLE "secrets"
(
    user_id integer UNIQUE REFERENCES users (id),
    secret  character(255) NOT NULL
);