SELECT "o0"."Id", "c"."Name", "o0"."PlacedAt"
FROM (
    SELECT "o"."Id", "o"."CustomerId", "o"."PlacedAt"
    FROM "Orders" AS "o"
    ORDER BY "o"."PlacedAt" DESC, "o"."Id" DESC
    LIMIT @p1 OFFSET @p
) AS "o0"
INNER JOIN "Customers" AS "c" ON "o0"."CustomerId" = "c"."Id"
ORDER BY "o0"."PlacedAt" DESC, "o0"."Id" DESC
