SELECT "o1"."Id", "o1"."CustomerId", "o1"."PlacedAt", "o0"."Id", "o0"."OrderId", "o0"."Product", "o0"."Quantity", "o0"."UnitPriceCents"
FROM (
    SELECT "o"."Id", "o"."CustomerId", "o"."PlacedAt"
    FROM "Orders" AS "o"
    WHERE "o"."Id" = @orderId
    LIMIT 2
) AS "o1"
LEFT JOIN "OrderLines" AS "o0" ON "o1"."Id" = "o0"."OrderId"
ORDER BY "o1"."Id", "o0"."Id"
